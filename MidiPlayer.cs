using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Midi;
using NBagOfTricks.Utils;


// TODO solo/mute individual drums.
// TODO incorporate fluidsynth?


namespace ClipPlayer
{
    /// <summary>
    /// A "good enough" midi player.
    /// There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
    /// ppq resolution and accuracy. The timer is also inherently wobbly.
    /// </summary>
    public partial class MidiPlayer : IPlayer
    {
        #region Constants
        /// <summary>Midi caps.</summary>
        const int NUM_CHANNELS = 16;

        /// <summary>Only 4/4 time supported.</summary>
        const int BEATS_PER_BAR = 4;

        /// <summary>Our ppq aka resolution aka ticks per beat. 4 gives 16th note, 8 gives 32nd note, etc.</summary>
        const int PPQ = 32;

        /// <summary>The drum channel.</summary>
        const int DRUM_CHANNEL = 10;
        #endregion

        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Indicates whether or not the midi is playing.</summary>
        bool _running = false;

        /// <summary>Midi output device.</summary>
        MidiOut _midiOut = null;

        /// <summary>Period.</summary>
        double _msecPerTick = 0;

        /// <summary>Current volume between 0 and 1.</summary>
        float _volume = 0.8f;

        /// <summary>Midi events from the input file.</summary>
        MidiEventCollection _sourceEvents = null;

        /// <summary>All the channels.</summary>
        readonly PlayChannel[] _playChannels = new PlayChannel[NUM_CHANNELS];

        /// <summary>Requested tempo from file.</summary>
        int _tempo = 100;

        /// <summary>Multimedia timer identifier.</summary>
        int _timerID = -1;

        /// <summary>Delegate for Windows mmtimer callback.</summary>
        delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        /// <summary>Called by Windows when a mmtimer event occurs.</summary>
        readonly TimeProc _timeProc;




        string _midiOutDevice = "";

        int _drumChannel = 1;

        bool _mapDrumChannel = false;

        bool _logEvents = false;

        /// <summary>Total length in ticks.</summary>
        int _length;

        /// <summary>First valid point.</summary>
        int _start;

        /// <summary>Last valid point.</summary>
        int _end;

        /// <summary>Current.</summary>
        int _current;

        #endregion


        #region Events
        /// <inheritdoc />
        public event EventHandler PlaybackCompleted;

        /// <inheritdoc />
        public event EventHandler<string> Log;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public MidiPlayer()
        {
            //chkMapDrums.Checked = Common.Settings.MapDrumChannel;
            //chkMapDrums.Text = $"Drums\r\nchan {Common.Settings.DrumChannel}";
            //BeatsPerBar = BEATS_PER_BAR;
            //TicksPerBeat = PPQ;
            //Snap = Common.Settings.Snap;

            // Figure out which midi output device.
            for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
            {
                if (_midiOutDevice == MidiOut.DeviceInfo(devindex).ProductName)
                {
                    _midiOut = new MidiOut(devindex);
                    break;
                }
            }

            if(_midiOut == null)
            {
//                MessageBox.Show($"Invalid midi device: {Common.Settings.MidiOutDevice}");
            }

            _timeProc = new TimeProc(MmTimerCallback);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Stop and destroy mmtimer.
            Stop();
            timeKillEvent(_timerID);

            // Resources.
            _midiOut?.Dispose();
            _midiOut = null;
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            bool ok = true;
            _fn = fn;

            // Clean up first.
            Rewind();

            if (ok)
            {
                // Default in case not specified in file.
                int lastTick = 0;
                _tempo = 100;
                HashSet<int> allDrums = new HashSet<int>();

                // Get events.
                var mfile = new MidiFile(fn, true);
                _sourceEvents = mfile.Events;

                // Init internal structure.
                for (int i = 0; i < _playChannels.Count(); i++)
                {
                    _playChannels[i] = new PlayChannel() { ChannelNumber = i + 1 };
                }

                // Bin events by channel. Scale ticks to internal ppq.
                for (int track = 0; track < _sourceEvents.Tracks; track++)
                {
                    foreach(var te in _sourceEvents.GetTrackEvents(track))
                    {
                        if (te.Channel - 1 < NUM_CHANNELS) // midi is one-based
                        {
                            // Do some miscellaneous fixups.

                            // Scale tick to internal.
                            long tick = te.AbsoluteTime * PPQ / _sourceEvents.DeltaTicksPerQuarterNote;

                            // Adjust channel for non-standard drums.
                            if (_mapDrumChannel && te.Channel == _drumChannel)
                            {
                                te.Channel = DRUM_CHANNEL;
                            }

                            // Other ops.
                            switch(te)
                            {
                                case NoteOnEvent non:
                                    // Collect drum list.
                                    if(non.Channel == DRUM_CHANNEL)
                                    {
                                        allDrums.Add(non.NoteNumber);
                                    }

                                    //if (non.Velocity > 0 && non.NoteLength == 0) EXP
                                    //{
                                    //    non.NoteLength = 1;
                                    //}
                                    break;

                                case PatchChangeEvent evt:
                                    _playChannels[te.Channel - 1].Patch = evt.Patch;
                                    break;
                            }

                            // Add to our collection.
                            _playChannels[te.Channel - 1].AddEvent((int)tick, te);
                        }
                    };
                }

                // Final fixups.
                for (int i = 0; i < _playChannels.Count(); i++)
                {
                    var pc = _playChannels[i];

                    pc.Name = $"Ch:({i + 1}) ";

                    if(i + 1 == DRUM_CHANNEL)
                    {
                        pc.Name += $"Drums";
                    }
                    else if (pc.Patch == -1)
                    {
                        pc.Name += $"NoPatch";
                    }
                    //else if(_instrumentDefs.ContainsKey(pc.Patch))
                    //{
                    //    pc.Name += $"{_instrumentDefs[pc.Patch]}";
                    //}
                    else
                    {
                        pc.Name += $"Patch:{pc.Patch}";
                    }

                    lastTick = Math.Max(lastTick, pc.MaxTick);
                    if(pc.Valid)
                    {
                    //    clickGrid.AddIndicator(pc.Name, i);
                    }
                }

                // Figure out times.
                _length = lastTick;
                _start = 0;
                _end = _length - 1;
                _current = 0;

                //// Debug stuff. EXP
                //List<string> sdbg = new List<string>() { "Drums:" };
                //foreach(int dr in allDrums)
                //{
                //    string snm = _drumDefs.ContainsKey(dr) ? _drumDefs[dr] : "???";
                //    sdbg.Add($"{dr}={snm}");
                //}
                //LogMessage(string.Join(" ", sdbg));
            }

            return ok;
        }

        /// <inheritdoc />
        public void Start()
        {
            // Start or restart?
            if(!_running)
            {
                timeKillEvent(_timerID);

                // Calculate the actual period to tell the user.
                double secPerBeat = 60 / _tempo;
                _msecPerTick = 1000 * secPerBeat / PPQ;

                int period = _msecPerTick > 1.0 ? (int)Math.Round(_msecPerTick) : 1;
                float msecPerBeat = period * PPQ;
                float actualBpm = 60.0f * 1000.0f / msecPerBeat;
                LogMessage($"Period:{period} Goal_BPM:{_tempo:f2} Actual_BPM:{actualBpm:f2}");

                // Create and start periodic timer. Resolution is 1. Mode is TIME_PERIODIC.
                _timerID = timeSetEvent(period, 1, _timeProc, IntPtr.Zero, 1);

                // If the timer was created successfully.
                if (_timerID != 0)
                {
                    _running = true;
                }
                else
                {
                    _running = false;
                    throw new Exception("Unable to start periodic multimedia Timer.");
                }
            }
            else
            {
                Rewind();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if(_running)
            {
                _running = false;
                timeKillEvent(_timerID);
                _timerID = -1;

                // Send midi stop all notes just in case.
                for (int i = 0; i < _playChannels.Count(); i++)
                {
                    if(_playChannels[i].Valid)
                    {
                        Kill(i);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Rewind()
        {
            _current = 0;
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// </summary>
        void MmTimerCallback(int id, int msg, int user, int param1, int param2)
        {
            if (_running)
            {
                // Any soloes?
                bool solo = _playChannels.Where(c => c.Mode == PlayChannel.PlayMode.Solo).Count() > 0;

                // Process each channel.
                foreach (var ch in _playChannels)
                {
                    if(ch.Valid)
                    {
                        // Look for events to send.
                        if (ch.Mode == PlayChannel.PlayMode.Solo || (!solo && ch.Mode == PlayChannel.PlayMode.Normal))
                        {
                            // Process any sequence steps.
                            if (ch.Events.ContainsKey(_current))
                            {
                                foreach (var mevt in ch.Events[_current])
                                {
                                    switch (mevt)
                                    {
                                        // Adjust volume.
                                        case NoteOnEvent evt:
                                            if (ch.ChannelNumber == DRUM_CHANNEL && evt.Velocity == 0)
                                            {
                                                // EXP - skip noteoffs as windows GM doesn't like them.
                                            }
                                            else
                                            {
                                                double vel = evt.Velocity;
                                                evt.Velocity = (int)(vel * _volume);
                                                MidiSend(evt);
                                                // Need to restore.
                                                evt.Velocity = (int)vel;
                                            }
                                            break;

                                        case NoteEvent evt:
                                            if(ch.ChannelNumber == DRUM_CHANNEL)
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
                        }
                    }
                }

                // Bump time. Check for end of play. Client will take care of transport control.
                bool done = false;

                _current += 1;
                if (_current < 0)
                {
                    _current = 0;
                }
                else if (_current >= _length)
                {
                    _current = _length - 1;
                    done = true;
                }
                else if (_current >= _end)
                {
                    _current = _end - 1;
                    done = true;
                }

                if (done)
                {
                    _running = false;
                    PlaybackCompleted?.Invoke(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void MidiSend(MidiEvent evt)
        {
            _midiOut?.Send(evt.GetAsShortMessage());

            if (_logEvents)
            {
                LogMessage(evt.ToString());
            }
        }

        /// <summary>
        /// Logger.
        /// </summary>
        /// <param name="s"></param>
        void LogMessage(string s)
        {
            Log?.Invoke(this, s);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channel"></param>
        void Kill(int channel)
        {
            //LogMessage($"Kill:{channel}");
            ControlChangeEvent nevt = new ControlChangeEvent(0, channel + 1, MidiController.AllNotesOff, 0);
            MidiSend(nevt);
        }

        /// <summary>
        /// Convert to musical representation.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        (int bar, int beat, int tick) Convert(int tick)
        {
            return (tick / BEATS_PER_BAR / PPQ, tick / PPQ % BEATS_PER_BAR, tick % PPQ);
        }
        #endregion

        #region Interop Multimedia Timer Functions
#pragma warning disable IDE1006 // Naming Styles

        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        /// <summary>Start her up.</summary>
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, IntPtr user, int mode);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        /// <summary>Represents information about the multimedia timer capabilities.</summary>
        [StructLayout(LayoutKind.Sequential)]
        struct TimerCaps
        {
            public int periodMin;
            public int periodMax;
        }
        #pragma warning restore IDE1006 // Naming Styles
        #endregion
    }

    /// <summary>Channel events and other properties.</summary>
    public class PlayChannel
    {
        #region Properties
        /// <summary>For UI.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For UI.</summary>
        public string Name { get; set; } = "";

        /// <summary>Channel used.</summary>
        public bool Valid { get { return Events.Count > 0; } }

        /// <summary>For muting/soloing.</summary>
        public PlayMode Mode { get; set; } = PlayMode.Normal;
        public enum PlayMode { Normal = 0, Solo = 1, Mute = 2 }

        /// <summary>For UI.</summary>
        public int Patch { get; set; } = -1;

        ///<summary>The main collection of Steps. The key is the subbeat/tick to send the list.</summary>
        public Dictionary<int, List<MidiEvent>> Events { get; set; } = new Dictionary<int, List<MidiEvent>>();

        ///<summary>The duration of the whole channel.</summary>
        public int MaxTick { get; private set; } = 0;
        #endregion

        /// <summary>Add an event at the given tick.</summary>
        /// <param name="tick"></param>
        /// <param name="evt"></param>
        public void AddEvent(int tick, MidiEvent evt)
        {
            if (!Events.ContainsKey(tick))
            {
                Events.Add(tick, new List<MidiEvent>());
            }
            Events[tick].Add(evt);
            MaxTick = Math.Max(MaxTick, tick);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"PlayChannel: Name:{Name} Number:{ChannelNumber} Mode:{Mode} Events:{Events.Count} MaxTick:{MaxTick}";
        }
    }
}
