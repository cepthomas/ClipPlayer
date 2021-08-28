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

        /// <summary>Our internal ppq aka resolution.</summary>
        const int PPQ = 32;

        /// <summary>Normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;
        #endregion

        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut _midiOut = null;

        /// <summary>The fast timer.</summary>
        MmTimerEx _mmTimer = new MmTimerEx();

        /// <summary>Period.</summary>
        double _msecPerSubdiv = 0;

        /// <summary>Midi events from the input file.</summary>
        MidiEventCollection _sourceEvents = null;

        ///<summary>The internal collection of events. The key is the subdiv/time to send the list.</summary>
        readonly Dictionary<int, List<MidiEvent>> _playEvents = new Dictionary<int, List<MidiEvent>>();

        /// <summary>Total length in subdivs.</summary>
        int _totalSubdivs;

        /// <summary>Current position in subdivs.</summary>
        int _currentSubdiv;

        /// <summary>Current tempo. Initialize to default in case the file doesn't supply one.</summary>
        int _tempo = Common.Settings.DefaultTempo;

        /// <summary>The midi instrument definitions. Enum value is the actual patch number.</summary>
        public enum InstrumentDef
        {
            AcousticGrandPiano = 0, BrightAcousticPiano, ElectricGrandPiano, HonkyTonkPiano, ElectricPiano1, ElectricPiano2,
            Harpsichord, Clavinet, Celesta, Glockenspiel, MusicBox, Vibraphone, Marimba, Xylophone, TubularBells,
            Dulcimer, DrawbarOrgan, PercussiveOrgan, RockOrgan, ChurchOrgan, ReedOrgan, Accordion, Harmonica,
            TangoAccordion, AcousticGuitarNylon, AcousticGuitarSteel, ElectricGuitarJazz, ElectricGuitarClean,
            ElectricGuitarMuted, OverdrivenGuitar, DistortionGuitar, GuitarHarmonics, AcousticBass, ElectricBassFinger,
            ElectricBassPick, FretlessBass, SlapBass1, SlapBass2, SynthBass1, SynthBass2, Violin, Viola, Cello,
            Contrabass, TremoloStrings, PizzicatoStrings, OrchestralHarp, Timpani, StringEnsemble1, StringEnsemble2,
            SynthStrings1, SynthStrings2, ChoirAahs, VoiceOohs, SynthVoice, OrchestraHit, Trumpet, Trombone, Tuba,
            MutedTrumpet, FrenchHorn, BrassSection, SynthBrass1, SynthBrass2, SopranoSax, AltoSax, TenorSax, BaritoneSax,
            Oboe, EnglishHorn, Bassoon, Clarinet, Piccolo, Flute, Recorder, PanFlute, BlownBottle, Shakuhachi, Whistle,
            Ocarina, Lead1Square, Lead2Sawtooth, Lead3Calliope, Lead4Chiff, Lead5Charang, Lead6Voice, Lead7Fifths,
            Lead8BassAndLead, Pad1NewAge, Pad2Warm, Pad3Polysynth, Pad4Choir, Pad5Bowed, Pad6Metallic, Pad7Halo, Pad8Sweep,
            Fx1Rain, Fx2Soundtrack, Fx3Crystal, Fx4Atmosphere, Fx5Brightness, Fx6Goblins, Fx7Echoes, Fx8SciFi, Sitar,
            Banjo, Shamisen, Koto, Kalimba, BagPipe, Fiddle, Shanai, TinkleBell, Agogo, SteelDrums, Woodblock,
            TaikoDrum, MelodicTom, SynthDrum, ReverseCymbal, GuitarFretNoise, BreathNoise, Seashore, BirdTweet,
            TelephoneRing, Helicopter, Applause, Gunshot
        }
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_totalSubdivs * _msecPerSubdiv)); } }

        /// <inheritdoc />
        public double Volume { get; set; }

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return new TimeSpan(0, 0, 0, 0, (int)(_currentSubdiv * _msecPerSubdiv)); }
            set { _currentSubdiv = (int)(value.TotalMilliseconds / _msecPerSubdiv); _currentSubdiv = MathUtils.Constrain(_currentSubdiv, 0, _totalSubdivs); }
        }
        #endregion

        #region Properties - other
        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        public int DrumChannel { get; set; } = DEFAULT_DRUM_CHANNEL;
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

            _currentSubdiv = 0;
            _totalSubdivs = 0;
            _playEvents.Clear();

            // Get events.
            var mfile = new MidiFile(fn, true);
            _sourceEvents = mfile.Events;

            // Scale to internal ppq.
            MidiTime mt = new MidiTime()
            {
                InternalPpq = PPQ,
                MidiPpq = _sourceEvents.DeltaTicksPerQuarterNote,
                Tempo = _tempo
            };

            for (int track = 0; track < _sourceEvents.Tracks; track++)
            {
                foreach (var te in _sourceEvents.GetTrackEvents(track))
                {
                    if (te.Channel - 1 < NUM_CHANNELS) // midi is one-based
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
            _msecPerSubdiv = 60.0 / _tempo;
            int period = mt.RoundedInternalPeriod();

            // Round length up to bar.
            int floor = _totalSubdivs / (PPQ * 4); // 4/4 only.
            _totalSubdivs = (floor + 1) * (PPQ * 4);

            // Create periodic timer.
            _mmTimer.SetTimer(period, MmTimerCallback);
            _mmTimer.Start();

            return true;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            int bars = _totalSubdivs / BEATS_PER_BAR / PPQ;
            int beats = _totalSubdivs / PPQ % BEATS_PER_BAR;
            int subdivs = _totalSubdivs % PPQ;

            string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} {bars + 1}:{beats + 1}:{subdivs}";
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
            _currentSubdiv = 0;
        }

        /// <inheritdoc />
        public void SettingsChanged()
        {
        }
        #endregion

        #region Public Functions - other
        /// <summary>
        /// Send a patch.
        /// </summary>
        /// <param name="channel">Substitute patch for this channel.</param>
        /// <param name="patch">Use this patch for Patch Channel.</param>
        public void SendPatch(int channel, int patch)
        {
            PatchChangeEvent evt = new PatchChangeEvent(0, channel, patch);
            MidiSend(evt);
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
                                    NoteOnEvent ne = new NoteOnEvent(
                                        evt.AbsoluteTime,
                                        evt.Channel == DrumChannel ? DEFAULT_DRUM_CHANNEL : evt.Channel,
                                        evt.NoteNumber,
                                        (int)(evt.Velocity * Volume),
                                        evt.OffEvent == null ? 0 : evt.NoteLength);
                                    MidiSend(ne);
                                }
                                break;

                            case NoteEvent evt:
                                if (evt.Channel == DrumChannel)
                                {
                                    // Skip drum noteoffs as windows GM doesn't like them.
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
                _currentSubdiv += 1;
                if (_currentSubdiv >= _totalSubdivs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
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
                Progress = _currentSubdiv < _totalSubdivs ? 100 * _currentSubdiv / _totalSubdivs : 100
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
