﻿using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;


namespace ClipPlayer
{
    /// <summary>
    /// A "good enough" midi player.
    /// There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
    /// ppq resolution and accuracy. The timer is also inherently wobbly.
    /// </summary>
    sealed class MidiClipPlayer : IPlayer
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        readonly IOutputDevice _outputDevice = new NullOutputDevice();

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Period.</summary>
        double _msecPerSubdiv = 0;

        /// <summary>Midi events from the input file.</summary>
        MidiEventCollection? _sourceEvents = null;

        ///<summary>The internal collection of events. The key is the subdiv/time to send the list.</summary>
        readonly Dictionary<int, List<MidiEvent>> _playEvents = new();

        /// <summary>Total length in subdivs.</summary>
        int _totalSubs;

        /// <summary>Current position in subdivs.</summary>
        int _currentSubdiv;

        /// <summary>Current tempo. Initialize to default in case the file doesn't supply one.</summary>
        int _tempo = Common.Settings.MidiSettings.DefaultTempo;
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_totalSubs * _msecPerSubdiv)); } }

        /// <inheritdoc />
        public double Volume { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _outputDevice.Valid; } }

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return new TimeSpan(0, 0, 0, 0, (int)(_currentSubdiv * _msecPerSubdiv)); }
            set { _currentSubdiv = (int)(value.TotalMilliseconds / _msecPerSubdiv); _currentSubdiv = MathUtils.Constrain(_currentSubdiv, 0, _totalSubs); }
        }
        #endregion

        #region Properties - other
        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        public int DrumChannel { get; set; } = MidiDefs.DEFAULT_DRUM_CHANNEL;
        #endregion

        #region Events - interface implementation
        /// <inheritdoc />
        public event EventHandler<StatusChangeEventArgs>? StatusChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public MidiClipPlayer()
        {
            // Set up output device.
            foreach (var dev in Common.Settings.MidiSettings.OutputDevices)
            {
                // Try midi.
                _outputDevice = new MidiOutput(dev.DeviceName);
                if (_outputDevice.Valid)
                {
                    break;
                }
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Stop and destroy mmtimer.
            State = RunState.Stopped;

            // Resources.
            _outputDevice.Dispose();

            _mmTimer.Stop();
            _mmTimer.Dispose();

            // Wait a bit in case there are some lingering events.
            System.Threading.Thread.Sleep(100);
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            _mmTimer.Stop();

            _currentSubdiv = 0;
            _totalSubs = 0;
            _playEvents.Clear();

            // Get events.
            var mfile = new MidiFile(fn, true);
            _sourceEvents = mfile.Events;

            // Scale to internal ppq.
            MidiTimeConverter mt = new(_sourceEvents.DeltaTicksPerQuarterNote, _tempo);
            for (int track = 0; track < _sourceEvents.Tracks; track++)
            {
                foreach (var te in _sourceEvents.GetTrackEvents(track))
                {
                    if (te.Channel - 1 < MidiDefs.NUM_CHANNELS) // midi is one-based
                    {
                        // Do some miscellaneous fixups.

                        // Scale to internal.
                        int subdiv = mt.MidiToInternal(te.AbsoluteTime);

                        // Other ops.
                        switch (te)
                        {
                            case NoteOnEvent non:
                                break;

                            case TempoEvent evt:
                                _tempo = (int)evt.Tempo;
                                break;
                        }

                        // Add to our collection.
                        if (!_playEvents.ContainsKey(subdiv))
                        {
                            _playEvents.Add(subdiv, new List<MidiEvent>());
                        }

                        _playEvents[subdiv].Add(te);
                        _totalSubs = Math.Max(_totalSubs, subdiv);
                    }
                };
            }

            State = RunState.Stopped;

            // Calculate the actual period.
            _msecPerSubdiv = mt.InternalToMsec(1);
            int period = mt.RoundedInternalPeriod();

            // Round total up to next beat.
            BarTime bt = new();
            bt.SetRounded(_totalSubs, SnapType.Beat, true);
            _totalSubs = Math.Max(_totalSubs, bt.TotalSubs);

            // Create periodic timer.
            _mmTimer.SetTimer(period, MmTimerCallback);
            _mmTimer.Start();

            return true;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            BarTime bt = new(_totalSubs);
            string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} ({bt.Bar}:{bt.Beat}:{bt.Sub:00})";
            return s;
        }

        /// <inheritdoc />
        public RunState Play()
        {
            State = RunState.Playing;
            return State;
        }

        /// <inheritdoc />
        public RunState Stop()
        {
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Kill(i + 1);
            }

            State = RunState.Stopped;
            return State;
        }

        /// <inheritdoc />
        public void Rewind()
        {
            _currentSubdiv = 0;
        }

        /// <inheritdoc />
        public void UpdateSettings()
        {
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            if (State == RunState.Playing)
            {
                if (_playEvents.ContainsKey(_currentSubdiv))
                {
                    // Process any sequence steps.
                    foreach (var mevt in _playEvents[_currentSubdiv])
                    {
                        switch (mevt)
                        {
                            // Adjust volume.
                            case NoteOnEvent evt:
                                if (evt.Channel == DrumChannel && evt.Velocity == 0)
                                {
                                    // Skip drum noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    // Adjust volume and maybe drum channel. Also NAudio NoteLength bug.
                                    NoteOnEvent ne = new(
                                        evt.AbsoluteTime,
                                        evt.Channel == DrumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                        evt.NoteNumber,
                                        (int)(evt.Velocity * Volume),
                                        evt.OffEvent is null ? 0 : evt.NoteLength);
                                    SendMidi(ne);
                                }
                                break;

                            case NoteEvent evt:
                                if (evt.Channel == DrumChannel)
                                {
                                    // Skip drum noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    SendMidi(evt);
                                }
                                break;

                            // No change.
                            default:
                                SendMidi(mevt);
                                break;
                        }
                    }
                }

                // Bump time. Check for end of play. Client will take care of transport control.
                _currentSubdiv += 1;
                if (_currentSubdiv >= _totalSubs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
                }

                StatusChange?.Invoke(this, new StatusChangeEventArgs()
                {
                    Progress = _currentSubdiv < _totalSubs ? 100 * _currentSubdiv / _totalSubs : 100
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void SendMidi(MidiEvent evt)
        {
            _outputDevice.SendEvent(evt);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channel">1-based channel</param>
        void Kill(int channel)
        {
            ControlChangeEvent nevt = new(0, channel, MidiController.AllNotesOff, 0);
            SendMidi(nevt);
        }
        #endregion
    }
}
