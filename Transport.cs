using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;


namespace ClipPlayer
{
    public partial class Transport : Form
    {
        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Audio device.</summary>
        WavePlayer _wavePlayer = null;

        /// <summary>Midi device.</summary>
        MidiPlayer _midiPlayer = null;

        /// <summary>Current play device.</summary>
        IPlayer _player = null;

        /// <summary>Listen for new instances.</summary>
        IpcServer _server = null;

        /// <summary>My logger.</summary>
        MpLog _log = new MpLog(Common.LogFileName, "TRNS");

        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Transport(string fn)
        {
            _fn = fn;
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
            bool ok = true;

            _log.Write($"CurrentDirectory:{Environment.CurrentDirectory}");
            _log.Write($"ExecutablePath:{Application.ExecutablePath}");
            _log.Write($"StartupPath:{Application.StartupPath}");

            // Get the settings.
            string appDir = MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera");
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);
            sldVolume.Value = Common.Settings.Volume;
            Location = Common.Settings.Position;

            if(!Common.Settings.ShowLog)
            {
                ClientSize = new Size(ClientRectangle.Width, progress.Bottom + 5);
            }

            // Create the playback devices.
            _midiPlayer = new MidiPlayer();
            _midiPlayer.StatusEvent += Player_StatusEvent;
            _wavePlayer = new WavePlayer();
            _wavePlayer.StatusEvent += Player_StatusEvent;

            // Hook up UI handlers.
            pbPlay.Click += (_, __) => { _player.Play(); };
            pbStop.Click += (_, __) => { _player.Stop(); };
            pbRewind.Click += (_, __) => { _player.Rewind(); progress.AddValue(0); };
            sldVolume.ValueChanged += (_, __) => { Common.Settings.Volume = sldVolume.Value; _player.Volume = sldVolume.Value; };

            // Go!
            ok = OpenFile();

            if(ok)
            {
                // Start listening for new app instances.
                _server = new IpcServer(Common.PIPE_NAME);
                _server.IpcServerEvent += Server_IpcEvent;
                _server.Start();
            }
            else
            {
                // Bail out.
                Environment.ExitCode = 1;
                Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Transport_FormClosing(object sender, FormClosingEventArgs e)
        {
            Common.Settings.Position = Location;
            Common.Settings.Save();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            _midiPlayer.Stop();
            _midiPlayer.Dispose();
            _midiPlayer = null;

            _wavePlayer.Stop();
            _wavePlayer.Dispose();
            _wavePlayer = null;

            _server.Kill();
            _server.Dispose();
            _server = null;

            base.Dispose(disposing);
        }
        #endregion

        #region Play file
        /// <summary>
        /// Open the file to play. Caller has already set _fn to requested file name.
        /// </summary>
        /// <returns></returns>
        bool OpenFile()
        {
            bool ok = true;

            _player?.Stop();

            try
            {
                switch (Path.GetExtension(_fn).ToLower())
                {
                    case ".mid":
                        _player = _midiPlayer;
                        break;

                    case ".wav":
                    case ".mp3":
                    case ".m4a":
                    case ".flac":
                        _player = _wavePlayer;
                        break;

                    default:
                        ShowMessage($"Invalid file: {_fn}", true);
                        ok = false;
                        _fn = "";
                        break;
                }

                if(ok)
                {
                    if (_player.OpenFile(_fn))
                    {
                        Text = $"{Path.GetFileName(_fn)} {_player.GetInfo()}";
                        _player.Volume = sldVolume.Value;
                        _player.Rewind();
                        _player.Play();
                    }
                    else
                    {
                        ShowMessage($"Couldn't open file", true);
                        _fn = "";
                        ok = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Fail open file: {ex}", true);
                _fn = "";
                ok = false;
            }

            if (_fn != "")
            {
                _log.Write($"File to play:{_fn}");
            }

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_StatusEvent(object sender, StatusEventArgs e)
        {
            if (e.Message != "")
            {
                _log.Write(e.Message);
            }

            switch (_player.State)
            {
                case RunState.Playing:
                    progress.AddValue(e.Progress);
                    break;

                case RunState.Stopped:
                    progress.AddValue(e.Progress);
                    break;

                case RunState.Complete:
                    if (Common.Settings.AutoClose)
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
            }
        }
        #endregion

        #region Pipe event handler
        /// <summary>
        /// Something has arrived.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Server_IpcEvent(object sender, IpcServerEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                switch (e.Status)
                {
                    case IpcServerStatus.Message:
                        _fn = e.Message;
                        OpenFile();
                        break;

                    case IpcServerStatus.Error:
                        _log.Write($"Server error:{e.Message}", true);
                        ShowMessage($"Server error:{e.Message}", false);
                        break;
                }
            });
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "User Settings",
                Size = new Size(300, 350),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                PropertyGrid pg = new PropertyGrid()
                {
                    Dock = DockStyle.Fill,
                    PropertySort = PropertySort.Categorized,
                    SelectedObject = Common.Settings
                };

                // Detect changes of interest.
                bool restart = false;

                pg.PropertyValueChanged += (sdr, args) =>
                {
                    restart |= args.ChangedItem.PropertyDescriptor.Name.EndsWith("Device");
                };

                f.Controls.Add(pg);
                f.ShowDialog();

                // Figure out what changed - each handled differently.
                if(restart)
                {
                    MessageBox.Show("Restart required for device changes to take effect");
                }

                Common.Settings.Save();
            }
        }
        #endregion

        #region Private functions
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
