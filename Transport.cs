using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;
using NBagOfUis;


namespace ClipPlayer
{
    public partial class Transport : Form
    {
        #region Fields
        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Audio device.</summary>
        WavePlayer? _wavePlayer = null;

        /// <summary>Midi device.</summary>
        MidiPlayer? _midiPlayer = null;

        /// <summary>Current play device.</summary>
        IPlayer? _player = null;

        /// <summary>Listen for new instances.</summary>
        NBagOfTricks.SimpleIpc.Server? _server = null;

        /// <summary>My logger.</summary>
        readonly NBagOfTricks.SimpleIpc.MpLog _log = new(Common.LogFileName, "TRNS");

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
        void Transport_Load(object? sender, EventArgs e)
        {
            Icon = Properties.Resources.croco;
            bool ok = true;

            _log.Write($"CurrentDirectory:{Environment.CurrentDirectory}");
            _log.Write($"ExecutablePath:{Application.ExecutablePath}");
            _log.Write($"StartupPath:{Application.StartupPath}");

            // Get the settings.
            string appDir = MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera");
            Common.Settings = (UserSettings)Settings.Load(appDir, typeof(UserSettings));
            sldVolume.Value = Common.Settings.Volume;
            var pos = Common.Settings.FormGeometry;
            Location = new(pos.X, pos.Y);

            progress.DrawColor = Common.Settings.ControlColor;
            sldVolume.DrawColor = Common.Settings.ControlColor;
            chkPlay.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            chkLoop.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            chkDrumsOn1.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;

            // Create the playback devices.
            _midiPlayer = new MidiPlayer();
            _midiPlayer.StatusEvent += Player_StatusEvent;
            _wavePlayer = new WavePlayer();
            _wavePlayer.StatusEvent += Player_StatusEvent;

            // Hook up UI handlers.
            chkPlay.CheckedChanged += (_, __) => { _ = chkPlay.Checked ? _player?.Play() : _player?.Stop(); };
            btnRewind.Click += (_, __) => { _player?.Rewind(); progress.AddValue(0); };
            sldVolume.ValueChanged += (_, __) => { Common.Settings.Volume = sldVolume.Value; if(_player is not null) _player.Volume = sldVolume.Value; };
            chkDrumsOn1.CheckedChanged += (_, __) => { if (_midiPlayer is not null) { _midiPlayer.DrumChannel = chkDrumsOn1.Checked ? 1 : 10; } };

            btnSettings.Click += Settings_Click;
            progress!.MouseDown += Progress_MouseDown;
            progress!.MouseMove += Progress_MouseMove;

            // Go!
            ok = OpenFile();

            if(ok)
            {
                // Start listening for new app instances.
                _server = new NBagOfTricks.SimpleIpc.Server(Common.PipeName, Common.LogFileName);
                _server.ServerEvent += Server_IpcEvent;
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
        void Transport_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Common.Settings.FormGeometry = new(Location, Size);
            Common.Settings.Save();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            _midiPlayer?.Stop();
            _midiPlayer?.Dispose();

            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();

            _server?.Stop();
            _server?.Dispose();

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
            chkDrumsOn1.Checked = false;
            chkPlay.Checked = false;

            try
            {
                switch (Path.GetExtension(_fn).ToLower())
                {
                    case ".mid":
                    // case ".sty": Use ClipExplorer for these 
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
                    if (_player!.OpenFile(_fn))
                    {
                        Text = $"{Path.GetFileName(_fn)} {_player.GetInfo()}";
                        _player.Volume = sldVolume.Value;
                        _player.Rewind();
                        // Make it go.
                        chkPlay.Checked = true;
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
        void Player_StatusEvent(object? sender, StatusEventArgs e)
        {
            if (e.Message != "")
            {
                _log.Write(e.Message);
            }

            switch (_player!.State)
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
                        _player.Current = TimeSpan.Zero;
                        progress.AddValue(0);
                        if (chkLoop.Checked)
                        {
                            _player.Play();
                        }
                        else
                        {
                            chkPlay.Checked = false;
                        }
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
        void Server_IpcEvent(object? sender, NBagOfTricks.SimpleIpc.ServerEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if(e.Error)
                {
                    ShowMessage($"Server error:{e.Message}", false);
                }
                else
                {
                    _fn = e.Message;
                    OpenFile();
                }
            });
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = Common.Settings.Edit("User Settings");

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                restart |= name.EndsWith("Device");
                restart |= name == "Latency";
                restart |= cat == "Cosmetics";
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            Common.Settings.Save();
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
            _log.Write(msg, true);
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
        void Progress_MouseDown(object? sender, MouseEventArgs e)
        {
            progress.AddValue(e.X * 100 / progress.Width);

            TimeSpan ts = GetTimeFromMouse(e.X);
            _player!.Current = ts;
            Invalidate();
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        void Progress_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.X != _lastXPos)
            {
                TimeSpan ts = GetTimeFromMouse(e.X);
                toolTip.SetToolTip(progress, ts.ToString(@"mm\:ss\.fff"));
                _lastXPos = e.X;
            }
        }

        /// <summary>
        /// Convert x pos to TimeSpan.
        /// </summary>
        /// <param name="x"></param>
        TimeSpan GetTimeFromMouse(int x)
        {
            int msec = x * (int)_player!.Length.TotalMilliseconds / progress.Width;
            msec = MathUtils.Constrain(msec, 0, (int)_player.Length.TotalMilliseconds);
            return new TimeSpan(0, 0, 0, 0, msec);
        }
        #endregion
    }
}
