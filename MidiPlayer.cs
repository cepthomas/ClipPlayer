using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Midi;
using NBagOfTricks;


namespace ClipPlayer
{
    /// <summary>
    /// A "good enough" midi player.
    /// There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
    /// ppq resolution and accuracy. The timer is also inherently wobbly.
    /// </summary>
    public class MidiPlayer : IPlayer
    {
        #region Constants
        /// <summary>Midi caps.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>Only 4/4 time supported.</summary>
        const int BEATS_PER_BAR = 4;

        /// <summary>Our ppq aka resolution.</summary>
        const int PPQ = 32;

        /// <summary>The drum channel.</summary>
        const int DRUM_CHANNEL = 10;
        #endregion

        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut _midiOut = null;

        /// <summary>The fast timer.</summary>
        MmTimerEx _mmTimer = new MmTimerEx();

        /// <summary>Period.</summary>
        double _msecPerTick = 0;

        /// <summary>Midi events from the input file.</summary>
        MidiEventCollection _sourceEvents = null;

        ///<summary>The internal collection of events. The key is the subbeat/tick to send the list.</summary>
        readonly Dictionary<int, List<MidiEvent>> _playEvents = new Dictionary<int, List<MidiEvent>>();

        /// <summary>Total length in ticks.</summary>
        int _totalTicks;

        /// <summary>Current position in ticks.</summary>
        int _currentTick;

        /// <summary>Current tempo.</summary>
        int _tempo = Common.Settings.DefaultTempo;
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_totalTicks * _msecPerTick)); } }

        /// <inheritdoc />
        public double Volume { get; set; }

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return new TimeSpan(0, 0, 0, 0, (int)(_currentTick * _msecPerTick)); }
            set { _currentTick = (int)(value.TotalMilliseconds / _msecPerTick); _currentTick = MathUtils.Constrain(_currentTick, 0, _totalTicks); }
        }
        #endregion

        #region Events - interface implementation
        /// <inheritdoc />
        public event EventHandler<StatusEventArgs> StatusEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public MidiPlayer()
        {
            // Figure out which midi output device.
            int devIndex = -1;
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (Common.Settings.MidiOutDevice == MidiOut.DeviceInfo(i).ProductName)
                {
                    devIndex = i;
                    break;
                }
            }
            _midiOut = new MidiOut(devIndex);

            if (_midiOut == null)
            {
                Tell($"Invalid midi device: {Common.Settings.MidiOutDevice}");
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
            _midiOut?.Dispose();
            _midiOut = null;

            _mmTimer?.Stop();
            _mmTimer?.Dispose();
            _mmTimer = null;
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            _mmTimer.Stop();

            _currentTick = 0;
            _totalTicks = 0;

            // Get events.
            var mfile = new MidiFile(fn, true);
            _sourceEvents = mfile.Events;

            // Scale ticks to internal ppq.
            for (int track = 0; track < _sourceEvents.Tracks; track++)
            {
                foreach(var te in _sourceEvents.GetTrackEvents(track))
                {
                    if (te.Channel - 1 < NUM_CHANNELS) // midi is one-based
                    {
                        // Do some miscellaneous fixups.

                        // Scale tick to internal.
                        int tick = (int)(te.AbsoluteTime * PPQ / _sourceEvents.DeltaTicksPerQuarterNote);

                        // Adjust channel for non-standard drums.
                        if (Common.Settings.DrumChannel > 0 && te.Channel == Common.Settings.DrumChannel)
                        {
                            te.Channel = DRUM_CHANNEL;
                        }

                        // Other ops.
                        switch(te)
                        {
                            case NoteOnEvent non:
                                break;

                            case TempoEvent evt:
                                _tempo = (int)evt.Tempo;
                                break;
                        }

                        // Add to our collection.
                        if (!_playEvents.ContainsKey(tick))
                        {
                            _playEvents.Add(tick, new List<MidiEvent>());
                        }

                        _playEvents[tick].Add(te);
                        _totalTicks = Math.Max(_totalTicks, tick);
                    }
                };
            }

            State = RunState.Stopped;

            // Calculate the actual period to tell the user.
            double secPerBeat = 60.0 / _tempo;
            _msecPerTick = 1000 * secPerBeat / PPQ;

            int period = _msecPerTick > 1.0 ? (int)Math.Round(_msecPerTick) : 1;
            float msecPerBeat = period * PPQ;
            float actualBpm = 60.0f * 1000.0f / msecPerBeat;
            Tell($"Period:{period} Goal_BPM:{_tempo:f2} Actual_BPM:{actualBpm:f2}");

            // Create periodic timer.
            _mmTimer.SetTimer(period, MmTimerCallback);
            _mmTimer.Start();

            return true;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            int bars = _totalTicks / BEATS_PER_BAR / PPQ;
            int beats = _totalTicks / PPQ % BEATS_PER_BAR;
            int ticks = _totalTicks % PPQ;

            string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} {bars + 1}:{beats + 1}:{ticks}";
            return s;
        }

        /// <inheritdoc />
        public void Play()
        {
            State = RunState.Playing;
        }

        /// <inheritdoc />
        public void Stop()
        {
            for (int i = 0; i < NUM_CHANNELS; i++)
            {
                Kill(i);
            }

            State = RunState.Stopped;
        }

        /// <inheritdoc />
        public void Rewind()
        {
            _currentTick = 0;
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
                // Process any sequence steps.
                if(_playEvents.ContainsKey(_currentTick))
                {
                    foreach (var mevt in _playEvents[_currentTick])
                    {
                        switch (mevt)
                        {
                            // Adjust volume.
                            case NoteOnEvent evt:
                                if (evt.Channel == DRUM_CHANNEL && evt.Velocity == 0)
                                {
                                    // EXP - skip noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    double vel = evt.Velocity;
                                    evt.Velocity = (int)(vel * Common.Settings.Volume);
                                    MidiSend(evt);
                                    // Need to restore.
                                    evt.Velocity = (int)vel;
                                }
                                break;

                            case NoteEvent evt:
                                if (evt.Channel == DRUM_CHANNEL)
                                {
                                    // EXP - skip noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    MidiSend(evt);
                                }
                                break;

                            // No change.
                            default:
                                MidiSend(mevt);
                                break;
                        }
                    }
                }

                // Bump time. Check for end of play. Client will take care of transport control.
                _currentTick += 1;
                if (_currentTick >= _totalTicks)
                {
                    State = RunState.Complete;
                    _currentTick = 0;
                }

                DoUpdate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void MidiSend(MidiEvent evt)
        {
            _midiOut.Send(evt.GetAsShortMessage());
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channel"></param>
        void Kill(int channel)
        {
            ControlChangeEvent nevt = new ControlChangeEvent(0, channel + 1, MidiController.AllNotesOff, 0);
            MidiSend(nevt);
        }

        /// <summary>
        /// Tell the mothership.
        /// </summary>
        void DoUpdate()
        {
            StatusEvent.Invoke(this, new StatusEventArgs()
            {
                Progress = _currentTick < _totalTicks ? 100 * _currentTick / _totalTicks : 100
            });
        }

        /// <summary>
        /// Tell the mothership.
        /// </summary>
        /// <param name="msg"></param>
        void Tell(string msg)
        {
            StatusEvent.Invoke(this, new StatusEventArgs()
            {
                Progress = 0,
                Message = msg
            });
        }
        #endregion
    }
}
