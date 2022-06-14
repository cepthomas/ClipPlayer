using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfTricks.Slog;
using MidiLib;


namespace ClipPlayer
{
    /// <summary>
    /// A "good enough" midi player.
    /// There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
    /// ppq resolution and accuracy. The timer is also inherently wobbly.
    /// </summary>
    public sealed class MidiClipPlayer : IPlayer
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiClipPlayer");

        /// <summary>Midi output device.</summary>
        readonly MidiSender _sender;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Period.</summary>
        double _msecPerSubdiv = 0;

        /// <summary>Midi events from the input file.</summary>
        MidiEventCollection? _sourceEvents = null;

        ///<summary>The internal collection of events. The key is the subdiv/time to send the list.</summary>
        readonly Dictionary<int, List<MidiEvent>> _playEvents = new();

        /// <summary>Total length in subdivs.</summary>
        int _totalSubdivs;

        /// <summary>Current position in subdivs.</summary>
        int _currentSubdiv;

        /// <summary>Current tempo. Initialize to default in case the file doesn't supply one.</summary>
        int _tempo = Common.Settings.DefaultTempo;
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_totalSubdivs * _msecPerSubdiv)); } }

        /// <inheritdoc />
        public double Volume { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _sender.Valid; } }

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return new TimeSpan(0, 0, 0, 0, (int)(_currentSubdiv * _msecPerSubdiv)); }
            set { _currentSubdiv = (int)(value.TotalMilliseconds / _msecPerSubdiv); _currentSubdiv = MathUtils.Constrain(_currentSubdiv, 0, _totalSubdivs); }
        }
        #endregion

        #region Properties - other
        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        public int DrumChannel { get; set; } = MidiDefs.DEFAULT_DRUM_CHANNEL;
        #endregion

        #region Events - interface implementation
        /// <inheritdoc />
        public event EventHandler<StatusEventArgs>? StatusEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public MidiClipPlayer()
        {
            _sender = new(Common.Settings.MidiOutDevice);
            _sender.LogMidi = false;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Stop and destroy mmtimer.
            State = RunState.Stopped;

            // Resources.
            _sender.Dispose();

            _mmTimer.Stop();
            _mmTimer.Dispose();
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            _logger.Info($"Open file:{fn}");

            _mmTimer.Stop();

            _currentSubdiv = 0;
            _totalSubdivs = 0;
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
                        _totalSubdivs = Math.Max(_totalSubdivs, subdiv);
                    }
                };
            }

            State = RunState.Stopped;

            // Calculate the actual period.
            _msecPerSubdiv = mt.InternalToMsec(1);
            int period = mt.RoundedInternalPeriod();

            // Round total up to next beat.
            BarSpan bs = new(0);
            bs.SetRounded(_totalSubdivs, SnapType.Beat, true);
            _totalSubdivs = Math.Max(_totalSubdivs, bs.TotalSubdivs);

            // Create periodic timer.
            _mmTimer.SetTimer(period, MmTimerCallback);
            _mmTimer.Start();

            return true;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            BarSpan bs = new(_totalSubdivs);
            int inc = Common.Settings.ZeroBased ? 0 : 1;
            string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} ({bs.Bar + inc}:{bs.Beat + inc}:{bs.Subdiv + inc:00})";
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
        public void SettingsChanged()
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
                if (_currentSubdiv >= _totalSubdivs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
                }

                StatusEvent?.Invoke(this, new StatusEventArgs()
                {
                    Progress = _currentSubdiv < _totalSubdivs ? 100 * _currentSubdiv / _totalSubdivs : 100
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void SendMidi(MidiEvent evt)
        {
            _sender.SendMidi(evt);
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
