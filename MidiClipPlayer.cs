using System;
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
        IOutputDevice? _outputDevice = null;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Period.</summary>
        double _msecPerSubbeat = 0;

        ///<summary>The internal collection of events. The key is the tick/time to send the list.</summary>
        readonly Dictionary<int, List<MidiEvent>> _playEvents = [];

        /// <summary>Total length in ticks.</summary>
        int _length;

        /// <summary>Current position in ticks.</summary>
        int _currentPosition;

        /// <summary>Current tempo. Initialize to default in case the file doesn't supply one.</summary>
        int _tempo = 100;
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_length * _msecPerSubbeat)); } }

        /// <inheritdoc />
        public double Volume { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _outputDevice is not null; } }

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return new TimeSpan(0, 0, 0, 0, (int)(_currentPosition * _msecPerSubbeat)); }
            set { _currentPosition = (int)(value.TotalMilliseconds / _msecPerSubbeat); _currentPosition = MathUtils.Constrain(_currentPosition, 0, _length); }
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
            _outputDevice = MidiManager.Instance.GetOutputDevice(Common.Settings.MidiDeviceName);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Stop and destroy mmtimer.
            State = RunState.Stopped;

            // Resources.
            MidiManager.Instance.DestroyDevices();
            _outputDevice = null;

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

            _currentPosition = 0;
            _length = 0;
            _playEvents.Clear();

            // Get events.
            var mfile = new MidiFile(fn, true);
            var sourceEvents = mfile.Events;

            // Store local.
            for (int trackNum = 0; trackNum < sourceEvents.Tracks; trackNum++)
            {
                foreach (var te in sourceEvents.GetTrackEvents(trackNum))
                {
                    // Scale to internal.
                    int subbeat = (int)te.AbsoluteTime * MusicTime.SubbeatsPerBeat / sourceEvents.DeltaTicksPerQuarterNote;

                    // Other ops.
                    switch (te)
                    {
                        case TempoEvent evt:
                            _tempo = (int)evt.Tempo;
                            break;
                    }

                    // Add to our collection.
                    _playEvents.AddLazy(subbeat, te);
                    _length = Math.Max(_length, subbeat);
                }
            }

            State = RunState.Stopped;

            // Calculate the actual period.
            _msecPerSubbeat = 1000 * (60.0 / _tempo) / MusicTime.SubbeatsPerBeat;

            // Round total up to next beat.
            MusicTime bt = new();
            bt.Set(_length, SnapType.Beat, true);
            _length = Math.Max(_length, bt.Tick);

            // Create periodic timer.
            _mmTimer.SetTimer(_msecPerSubbeat > 1.0 ? (int)Math.Round(_msecPerSubbeat) : 1, MmTimerCallback);
            _mmTimer.Start();

            return true;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            MusicTime bt = new(_length);
            var (bar, beat, tick) = bt.Parts;
            string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} ({bar}:{beat}:{tick:00})";
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
            MidiManager.Instance.Kill();
            State = RunState.Stopped;
            return State;
        }

        /// <inheritdoc />
        public void Rewind()
        {
            _currentPosition = 0;
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
                if (_playEvents.TryGetValue(_currentPosition, out List<MidiEvent>? value))
                {
                    // Process any sequence steps.
                    foreach (var mevt in value)
                    {
                        switch (mevt)
                        {
                            case NoteOnEvent evt:
                                if (evt.Channel == DrumChannel && evt.Velocity == 0)
                                {
                                    // Skip drum noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    // Adjust volume and maybe drum channel. Also NAudio NoteLength bug.
                                    NoteOn non = new(evt.Channel = evt.Channel, evt.NoteNumber, (int)(evt.Velocity * Volume), new(evt.AbsoluteTime));
                                    SendMidi(non);
                                }
                                break;

                            case NoteEvent evt:
                                if (evt.Channel == DrumChannel)
                                {
                                    // Skip drum noteoffs as windows GM doesn't like them.
                                }
                                else
                                {
                                    NoteOff noff = new(evt.Channel = evt.Channel, evt.NoteNumber, new(evt.AbsoluteTime));
                                    SendMidi(noff);
                                }
                                break;

                            case PatchChangeEvent evt:
                                Patch pt = new(evt.Channel, evt.Patch, new(evt.AbsoluteTime));
                                SendMidi(pt);
                                break;

                            case ControlChangeEvent evt:
                                Controller ctrl = new(evt.Channel, (int)evt.Controller, evt.ControllerValue, new(evt.AbsoluteTime));
                                SendMidi(ctrl);
                                break;

                            // All others ignore.
                            default:
                                //var smmmm = mevt.GetAsShortMessage();
                                //Other other = new(mevt.Channel, mevt.GetAsShortMessage(), new(mevt.AbsoluteTime));
                                //SendMidi(other);
                                break;
                        }
                    }
                }

                // Bump time. Check for end of play. Client will take care of transport control.
                _currentPosition += 1;
                if (_currentPosition >= _length)
                {
                    State = RunState.Complete;
                    _currentPosition = 0;
                }

                StatusChange?.Invoke(this, new StatusChangeEventArgs()
                {
                    Progress = _currentPosition < _length ? 100 * _currentPosition / _length : 100
                });
            }
        }

        /// <summary>
        /// Send it.
        /// </summary>
        /// <param name="evt"></param>
        void SendMidi(BaseEvent evt)
        {
            _outputDevice?.Send(evt);
        }
        #endregion
    }
}
