using System;
using System.Collections.Generic;
using System.IO;
using NBagOfTricks.CommandProcessor;
using NBagOfTricks.Utils;

//TODO how to stop/kill?

namespace ClipPlayer
{
    public class App
    {
        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Current play device.</summary>
        IPlayer _player = null;

        /// <summary>Current play state.</summary>
        RunState _state = RunState.Stopped;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public App()
        {
            // Create the specs for the command processor.
            var cp = new Processor();

            cp.Commands = new Commands
            {
                {
                    "",
                    "play a file: .mid|.wav|.mp3",
                    new Arguments
                    {
                        ///// Common
                        { 
                            "vol",
                            "volume from 0 to 1",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) => 
                            {
                                bool aok = double.TryParse(v, out double d);
                                if(aok) Common.Volume = (float)MathUtils.Constrain(d, 0, 1);
                                return aok;
                            }
                        },
                        ///// Wave player
                        {
                            "wdev",
                            "wav device",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) =>
                            {
                                Common.WavOutDevice = v;
                                return true;
                            }
                        },
                        {
                            "lat",
                            "latency",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) =>
                            {
                                bool aok = int.TryParse(v, out int i);
                                if(aok) Common.Latency = MathUtils.Constrain(i, 5, 500);
                                return aok;
                            }
                        },
                        ///// Midi player
                        {
                            "mdev",
                            "midi device",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) =>
                            {
                                Common.MidiOutDevice = v;
                                return true;
                            }
                        },
                        {
                            "drch",
                            "map this channel to drum channel",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) =>
                            {
                                bool aok = int.TryParse(v, out int i);
                                if(aok) Common.DrumChannel = MathUtils.Constrain(i, 1, 16);
                                return aok;
                            }
                        },
                        {
                            "tmp",
                            "tempo/bpm if not in file",
                            ArgOptType.Opt, ArgOptType.Req,
                            (v) =>
                            {
                                bool aok = int.TryParse(v, out int i);
                                if(aok) Common.Tempo = MathUtils.Constrain(i, 30, 250);
                                return aok;

                            }
                        }
                    },
                    // Files func
                    (v) =>
                    {
                        _fn = v.ToLower();
                        return File.Exists(_fn) && ".wav.mp3.mid".Contains(Path.GetExtension(_fn));
                    }
                }
            };

            /////// Processing ///////
            //-vol 0.9 -tmp 95 -drch 1 "C:\Dev\repos\ClipExplorer\_files\01 8th Hat.mid"
            //-vol 0.9 "C:\Dev\repos\ClipExplorer\_files\one-sec.wav"
            //change the Output type from Console Application to Windows Application.
            bool ok = cp.Parse(Environment.CommandLine, true);

            if(ok && _fn != "")
            {
                try
                {
                    switch(Path.GetExtension(_fn).ToLower())
                    {
                        case ".mid":
                            _player = new MidiPlayer();
                            break;

                        case ".wav":
                        case ".mp3":
                            _player = new WavePlayer();
                            break;
                    }

                    if (_player != null)
                    {

                        if (_player.OpenFile(_fn))
                        {
                            _player.StatusEvent += Player_StatusEvent;
                            _state = RunState.Runnning;
                            _player.Play();

                            // Wait until done.
                            while(_state == RunState.Runnning)
                            {
                                System.Threading.Thread.Sleep(50);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Couldn't open file");
                            Environment.ExitCode = 2;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Fail: {e}");
                    Environment.ExitCode = 3;
                }
                finally
                {
                    _player?.Dispose();
                }
            }
            else
            {
                Console.WriteLine(cp.GetUsage());
                Environment.ExitCode = 1;
            }
        }

        private void Player_StatusEvent(object sender, StatusEventArgs e)
        {
            _state = e.State;

            if(e.Message != "")
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
