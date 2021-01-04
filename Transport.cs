using NBagOfTricks.CommandProcessor;
using NBagOfTricks.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ClipPlayer
{
    public partial class Transport : Form
    {
        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Current play device.</summary>
        IPlayer _player = null;

        /// <summary>Report to system.</summary>
        int _exitCode = 0;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Transport()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Transport_Load(object sender, EventArgs e)
        {
            Icon = Properties.Resources.croco;
            Visible = true;

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
                        _fn = v;
                        return File.Exists(_fn) && ".wav.mp3.mid".Contains(Path.GetExtension(_fn.ToLower()));
                    }
                }
            };

            /////// Processing ///////
            //-vol 0.9 -tmp 95 -drch 1 "C:\Dev\repos\ClipExplorer\_files\01 8th Hat.mid"
            //-vol 0.9 "C:\Dev\repos\ClipExplorer\_files\one-sec.wav"
            //change the Output type from Console Application to Windows Application.
            bool ok = cp.Parse(Environment.CommandLine, true);

            if (ok && _fn != "")
            {
                try
                {
                    switch (Path.GetExtension(_fn).ToLower())
                    {
                        case ".mid":
                            _player = new MidiPlayer();
                            break;

                        case ".wav":
                        case ".mp3":
                            _player = new WavePlayer();
                            break;

                        default:
                            _exitCode = 4;
                            ShowMessage($"Invalid file: {_fn}");
                            break;
                    }

                    if (_player != null)
                    {
                        if (_player.OpenFile(_fn))
                        {
                            Text = $"{Path.GetFileName(_fn)} {_player.GetInfo()}";
                            _player.StatusEvent += Player_StatusEvent;
                            _player.Play();
                        }
                        else
                        {
                            _exitCode = 2;
                            ShowMessage($"Couldn't open file");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _exitCode = 3;
                    ShowMessage($"Fail: {ex}");
                }
            }
            else
            {
                _exitCode = 1;
                ShowMessage(cp.GetUsage());
            }

            if(_exitCode > 0)
            {
                // Bail out.
                ShutDown();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Transport_FormClosing(object sender, FormClosingEventArgs e)
        {
            ShutDown();
        }

        /// <summary>
        /// 
        /// </summary>
        void ShutDown()
        {
            _player?.Dispose();
            _player = null;
            Environment.Exit(_exitCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_StatusEvent(object sender, StatusEventArgs e)
        {
            switch(_player.State)
            {
                case RunState.Playing:
                    progress.AddValue(e.Progress);
                    break;

                case RunState.Stopped:
                    progress.AddValue(e.Progress);
                    break;

                case RunState.Complete:
                    ShutDown();
                    break;

                case RunState.Error:
                    ShowMessage(e.Message);
                    break;
            }
        }

        /// <summary>
        /// Show message then exit.
        /// </summary>
        /// <param name="msg"></param>
        void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
            ShutDown();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Play_Click(object sender, EventArgs e)
        {
            _player.Play();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Stop_Click(object sender, EventArgs e)
        {
            _player.Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rewind_Click(object sender, EventArgs e)
        {
            _player.Rewind();
            progress.AddValue(0);
        }
    }
}
