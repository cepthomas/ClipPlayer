using NBagOfTricks.CommandProcessor;
using System;
using System.Collections.Generic;
using System.IO;


namespace ClipPlayer
{

    public class App
    {
        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Current play device.</summary>
        IPlayer _player = null;
        #endregion


        bool _running = false;





        /// <summary>
        /// Constructor.
        /// </summary>
        public App()
        {
            // Create the specs for the command processor.
            var cp = new Processor();
            var args = Environment.GetCommandLineArgs();
            // ClipPlayer.exe  play -log -mdev "my midi device" -tmp 99

            cp.Commands = new Commands
            {
                {
                    "",
                    "",
                    new Arguments
                    {
                        ///// Common
                        { "vol", "volume from 0 to 1",                  ArgOptType.Opt, ArgOptType.Req,
                            (v) => { return float.TryParse(v, out _volume); } },
                        { "log", "log events",                          ArgOptType.Opt, ArgOptType.None,
                            (v) => { Common.LogEvents = true; return true; } },
                        ///// Wave player
                        { "wdev", "wav device",                         ArgOptType.Opt, ArgOptType.Req,
                            (v) => { Common.WavOutDevice = v; return true; } },
                        { "lat", "latency",                             ArgOptType.Opt, ArgOptType.Req,
                            (v) => { return int.TryParse(v, out _latency); } },
                        ///// Midi player
                        { "mdev", "midi device",                        ArgOptType.Opt, ArgOptType.Req,
                            (v) => { Common.MidiOutDevice = v; return true; } },
                        { "drch", "map this channel to drum channel",   ArgOptType.Opt, ArgOptType.Req,
                            (v) => { return int.TryParse(v, out _drumChannel); } },
                        { "tmp", "tempo aka bpm",                       ArgOptType.Opt, ArgOptType.Req,
                            (v) => { return int.TryParse(v, out _tempo); int.Parse  } }
                    },
                    // Files func
                    (v) =>
                    {
                        _fn = v.ToLower();
                        return ".wav.mp3.mid".Contains(Path.GetExtension(_fn));
                    }
                }
            };

            /////// Basic processing ///////
            int ecode = 0;
            string testCmd = "realcmd -def -jkl some1 -ghi -abc some2 InputFile1.txt InputFile2.doc InputFile3.doc";
            bool ok = cp.Parse("TODO testCmd");

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
                            _player.PlaybackCompleted += (_, __) => _running = false;
                            _player.Log += (sender, msg) => Console.WriteLine($"> ({sender}) {msg}");
                            _running = true;
                            _player.Start();

                            // TODO wait until done.

                        }
                        else
                        {
                            Console.WriteLine($"Couldn't open file");
                            ecode = 1;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Fail: {e}");
                    ecode = 2;
                }
                finally
                {
                    _player?.Dispose();
                }
            }

            Environment.Exit(ecode);
        }
    }
}
