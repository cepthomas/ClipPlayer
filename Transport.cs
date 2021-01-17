﻿using NAudio.Midi;
using NAudio.Wave;
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

        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Transport()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Transport_Load(object sender, EventArgs e)
        {
            Icon = Properties.Resources.croco;
            Visible = true;

            // Get defaults first.
            bool ok = ReadDefaults();
            if(!ok)
            {
                ShowMessage("Something wrong with the defaltss.txt file", true);
            }

            // Do command line processor.
            var cp = new Processor();

            if (ok)
            {
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
                                    return ValidateWaveDevice(v);
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
                                    return ValidateMidiDevice(v);
                                }
                            },
                            {
                                "drch",
                                "map this channel to drum channel",
                                ArgOptType.Opt, ArgOptType.Req,
                                (v) =>
                                {
                                    bool aok = int.TryParse(v, out int i);
                                    if(aok) Common.DrumChannel = MathUtils.Constrain(i, 1, MidiPlayer.NUM_CHANNELS);
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

                ok = cp.Parse(Environment.CommandLine, true);
            }

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
                            Environment.ExitCode = 4;
                            ShowMessage($"Invalid file: {_fn}", true);
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
                            Environment.ExitCode = 2;
                            ShowMessage($"Couldn't open file", true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.ExitCode = 3;
                    ShowMessage($"Fail: {ex}", true);
                }
            }
            else
            {
                Environment.ExitCode = 1;
                ShowMessage(cp.GetUsage(), true);
            }

            if(Environment.ExitCode > 0)
            {
                // Bail out.
                Environment.Exit(Environment.ExitCode);
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool ReadDefaults()
        {
            bool valid = true;
            const string DEF_FILE = "defaults.txt";

            if (File.Exists(DEF_FILE))
            {
                foreach(string s in File.ReadAllLines(DEF_FILE))
                {
                    if(!s.StartsWith("#"))
                    {
                        try
                        {
                            var parts = s.SplitByToken(":");
                            switch (parts[0].ToLower())
                            {
                                case "volume":
                                    Common.Volume = (float)MathUtils.Constrain(float.Parse(parts[1]), 0, 1);
                                    break;

                                case "wavoutdevice":
                                    if (!ValidateWaveDevice(parts[1].Replace("\"", "")))
                                    {
                                        valid = false;
                                    }
                                    break;

                                case "latency":
                                    Common.Latency = MathUtils.Constrain(int.Parse(parts[1]), 5, 500);
                                    break;

                                case "midioutdevice":
                                    if(!ValidateMidiDevice(parts[1].Replace("\"", "")))
                                    {
                                        valid = false;
                                    }
                                    break;

                                case "drumchannel":
                                    Common.DrumChannel = MathUtils.Constrain(int.Parse(parts[1]), 0, MidiPlayer.NUM_CHANNELS);
                                    break;

                                case "autoclose":
                                    Common.AutoClose = bool.Parse(parts[1]);
                                    break;

                                case "tempo":
                                    Common.Tempo = MathUtils.Constrain(int.Parse(parts[1]), 30, 250);
                                    break;

                                default:
                                    throw new Exception();
                            }

                        }
                        catch (Exception) // formatting errors etc.
                        {
                            valid = false;
                        }
                    }
                }
            }
            // else just use internal defaults.

            return valid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        bool ValidateWaveDevice(string v)
        {
            // Get available devices and check selection for sanity.
            bool valid = false;
            var rec = new List<string>();

            for (int id = -1; id < WaveOut.DeviceCount; id++) // –1 indicates the default output device, while 0 is the first output device
            {
                var cap = WaveOut.GetCapabilities(id);
                rec.Add(cap.ProductName);
            }

            if (rec.Contains(v))
            {
                Common.WavOutDevice = v;
                valid = true;
            }
            else
            {
                rec.Insert(0, "Invalid wave device - must be one of:");
                ShowMessage(string.Join(Environment.NewLine, rec), false);
            }

            return valid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        bool ValidateMidiDevice(string v)
        {
            // Get available devices and check selection for sanity.
            bool valid = false;
            var rec = new List<string>();

            for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
            {
                rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
            }

            if (rec.Contains(v))
            {
                Common.MidiOutDevice = v;
                valid = true;
            }
            else
            {
                rec.Insert(0, "Invalid midi device - must be one of:");
                ShowMessage(string.Join(Environment.NewLine, rec), false);
            }

            return valid;
        }

        /// <summary>
        /// Show message then optionally exit.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exit"></param>
        void ShowMessage(string msg, bool exit)
        {
            MessageBox.Show(msg);
            if(exit)
            {
                Environment.Exit(Environment.ExitCode);
            }
        }
        #endregion

        #region Event handlers
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
                    if(Common.AutoClose)
                    {
                        Environment.Exit(Environment.ExitCode);
                    }
                    else
                    {
                        _player.State = RunState.Stopped;
                        _player.Current = TimeSpan.Zero;
                        progress.AddValue(0);
                    }
                    break;

                case RunState.Error:
                    ShowMessage(e.Message, true);
                    break;
            }
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
        #endregion

        #region Mouse processing
        /// <summary>
        /// Handle dragging.
        /// </summary>
        void Progress_MouseDown(object sender, MouseEventArgs e)
        {
            TimeSpan ts = GetTimeFromMouse(e.X);
            _player.Current = ts;
            Invalidate();
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        void Progress_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.X != _lastXPos)
            {
                TimeSpan ts = GetTimeFromMouse(e.X);
                toolTip.SetToolTip(progress, ts.ToString(Common.TS_FORMAT));
                _lastXPos = e.X;
            }
        }

        /// <summary>
        /// Convert x pos to TimeSpan.
        /// </summary>
        /// <param name="x"></param>
        TimeSpan GetTimeFromMouse(int x)
        {
            int msec = x * (int)_player.Length.TotalMilliseconds / progress.Width;
            msec = MathUtils.Constrain(msec, 0, (int)_player.Length.TotalMilliseconds);
            return new TimeSpan(0, 0, 0, 0, msec);
        }
        #endregion
    }
}