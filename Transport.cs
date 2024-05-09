using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.AudioLib;
using Ephemera.MidiLib;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace ClipPlayer
{
    public partial class Transport : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Transport");

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Audio device.</summary>
        readonly AudioClipPlayer _audioPlayer;

        /// <summary>Midi device.</summary>
        readonly MidiClipPlayer _midiPlayer;

        /// <summary>Default device.</summary>
        readonly NullPlayer _nullPlayer;

        /// <summary>Current play device.</summary>
        IPlayer _player;

        /// <summary>Listen for new instances.</summary>
        NBagOfTricks.SimpleIpc.Server? _server;

        // /// <summary>My multiprocess logger for debug.</summary>
        // readonly NBagOfTricks.SimpleIpc.MpLog _log = new(Common.LogFileName, "TRNS");

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

            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera");
            Common.Settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            // Tell the libs about their settings.
            MidiSettings.LibSettings = Common.Settings.MidiSettings;
            AudioSettings.LibSettings = Common.Settings.AudioSettings;

            InitializeComponent();

            Icon = Properties.Resources.croco;

            sldVolume.Value = Common.Settings.Volume;
            var pos = Common.Settings.FormGeometry;
            Location = new(pos.X, pos.Y);

            // Init logging.
            LogManager.MinLevelFile = Common.Settings.FileLogLevel;
            LogManager.MinLevelNotif = Common.Settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run();

            // Create the playback devices.
            _midiPlayer = new();
            _midiPlayer.StatusChange += Player_StatusChange;
            _audioPlayer = new();
            _audioPlayer.StatusChange += Player_StatusChange;
            _nullPlayer = new();
            _nullPlayer.StatusChange += Player_StatusChange;
            _player = _nullPlayer;

            // Cosmetics.
            progress.DrawColor = Common.Settings.ControlColor;
            sldVolume.DrawColor = Common.Settings.ControlColor;
            chkPlay.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            chkLoop.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;

            // Hook up UI handlers.
            chkPlay.CheckedChanged += (_, __) => { _ = chkPlay.Checked ? _player.Play() : _player.Stop(); };
            btnRewind.Click += (_, __) => { _player.Rewind(); progress.AddValue(0); };
            sldVolume.ValueChanged += (_, __) => { _player.Volume = sldVolume.Value; };
            btnSettings.Click += Settings_Click;
            progress!.MouseDown += Progress_MouseDown;
            progress!.MouseMove += Progress_MouseMove;

            // Drum channel selection.
            cmbDrumChannel.BackColor = Common.Settings.ControlColor;
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                cmbDrumChannel.Items.Add($"{i + 1}");
            }
            cmbDrumChannel.SelectedIndexChanged += (_, __) => _midiPlayer.DrumChannel = cmbDrumChannel.SelectedIndex + 1;
            cmbDrumChannel.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL - 1;

            if(!Common.Settings.Debug)
            {
                ClientSize = new(ClientSize.Width, rtbLog.Top);
            }
        }

        /// <summary>
        /// Form is legal now. Init things that want to log.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _logger.Info($"OK to log now!!");
            //_log.Write($"CurrentDirectory:{Environment.CurrentDirectory}");
            //_log.Write($"ExecutablePath:{Application.ExecutablePath}");
            //_log.Write($"StartupPath:{Application.StartupPath}");

            bool ok = true;

            if(ok)
            {
                // Go!
                ok = OpenFile();
            }

            if(ok)
            {
                // Start listening for new app instances.
                _server = new NBagOfTricks.SimpleIpc.Server(Common.PipeName, Common.LogFileName);
                _server.IpcReceive += Server_IpcReceive;
                _server.Start();
            }
            else
            {
                //// Bail out?
                //Environment.ExitCode = 1;
                //Close();
            }
            base.OnLoad(e);
        }

        /// <summary>
        /// Goodbye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Common.Settings.FormGeometry = new(Location, Size);
            Common.Settings.Save();
            LogManager.Stop();

            base.OnFormClosing(e);
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

            _midiPlayer.Stop();
            _midiPlayer.Dispose();

            _audioPlayer.Stop();
            _audioPlayer.Dispose();

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
            chkPlay.Checked = false;
            cmbDrumChannel.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL - 1; // reset

            try
            {
                switch (Path.GetExtension(_fn).ToLower())
                {
                    case ".mid":
                        if (!_midiPlayer.Valid)
                        {
                            _logger.Error("Invalid midi output device");
                            MessageBox.Show("Invalid midi output device");
                            ok = false;
                        }
                        else
                        {
                            _player = _midiPlayer;
                        }
                        break;

                    case ".wav":
                    case ".mp3":
                    case ".m4a":
                    case ".flac":
                        if (!_audioPlayer.Valid)
                        {
                            _logger.Error("Invalid audio output device");
                            MessageBox.Show("Invalid audio output device");
                            ok = false;
                        }
                        else
                        {
                            _player = _audioPlayer;
                        }
                        break;

                    default:
                        MessageBox.Show($"Invalid file: {_fn}");
                        _player = _nullPlayer;
                        ok = false;
                        _fn = "";
                        break;
                }

                if(ok)
                {
                    if (_player.OpenFile(_fn))
                    {
                        Text = $"{Path.GetFileName(_fn)} {_player.GetInfo()}";
                        _logger.Info($"Open file:{_fn}");
                        _player.Volume = sldVolume.Value;
                        _player.Rewind();
                        // Make it go.
                        chkPlay.Checked = true;
                    }
                    else
                    {
                        MessageBox.Show("Couldn't open file");
                        _fn = "";
                        ok = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fail open file: {ex.Message}");
                _fn = "";
                ok = false;
            }

            if (_fn != "")
            {
                _logger.Info($"File to play:{_fn}");
            }

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_StatusChange(object? sender, StatusChangeEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if (e.Error != "")
                {
                    _logger.Error($"Player error: {e.Error}");
                    progress.AddValue(0);
                    chkPlay.Checked = false;
                    return; // early return!
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
            });
        }
        #endregion

        #region Pipe event handler
        /// <summary>
        /// Something has arrived.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? sender, NBagOfTricks.SimpleIpc.IpcReceiveEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if(e.Error)
                {
                    _logger.Warn($"Server error:{e.Message}");
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
            var changes = SettingsEditor.Edit(Common.Settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "WavOutDevice":
                    case "Latency":
                    case "InputDevice":
                    case "OutputDevice":
                    case "ControlColor":
                        restart = true;
                        break;
                }
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
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            // Usually come from a different thread.
            if (IsHandleCreated)
            {
                this.InvokeIfRequired(_ =>
                {
                    rtbLog.AppendText($"> {e.Message}{Environment.NewLine}");
                    rtbLog.ScrollToCaret();
                });
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
            _player.Current = ts;
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
            int msec = x * (int)_player.Length.TotalMilliseconds / progress.Width;
            msec = MathUtils.Constrain(msec, 0, (int)_player.Length.TotalMilliseconds);
            return new TimeSpan(0, 0, 0, 0, msec);
        }
        #endregion
    }
}
