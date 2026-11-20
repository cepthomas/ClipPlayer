using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Ephemera.AudioLib;
using Ephemera.MidiLib;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace ClipPlayer
{
    public partial class Transport : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Main");

        /// <summary>Settings.</summary>
        UserSettings _settings;

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Audio device.</summary>
        readonly AudioClipPlayer _audioPlayer;// = new();

        /// <summary>Midi device.</summary>
        readonly MidiClipPlayer _midiPlayer;// = new();

        /// <summary>Default device.</summary>
        readonly NullPlayer _nullPlayer = new();

        /// <summary>Current play device.</summary>
        IPlayer _player;

        /// <summary>Listen for new instances.</summary>
        Ipc.Server? _server;

        // /// <summary>My multiprocess logger for debug.</summary>
        // readonly NBagOfTricks.SimpleIpc.MpLog _log = new(Common.LogFileName, "TRNS");

        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Ready or not.</summary>
        bool _valid = true;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Transport(string fn)
        {
            try
            {
                _fn = fn;

                // Load settings first before initializing.
                string appDir = MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera");
                _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

                InitializeComponent();

                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

                sldVolume.Value = _settings.Volume;
                var pos = _settings.FormGeometry;
                Location = new(pos.X, pos.Y);

                // Init logging.
                LogManager.MinLevelFile = _settings.FileLogLevel;
                LogManager.MinLevelNotif = _settings.NotifLogLevel;
                LogManager.LogMessage += LogManager_LogMessage;
                LogManager.Run(Path.Join(appDir, "log.txt"), 100000);

                _logger.Info("Transport started");

                // Init the playback devices.
                _midiPlayer = new(_settings);
                _midiPlayer.ProgressChange += Player_ProgressChange;
                _audioPlayer = new(_settings);
                _audioPlayer.ProgressChange += Player_ProgressChange;

                // Default.
                _nullPlayer = new();
                _nullPlayer.ProgressChange += Player_ProgressChange;
                _player = _nullPlayer;

                // Cosmetics.
                progress.DrawColor = _settings.DrawColor;
                sldVolume.DrawColor = _settings.DrawColor;
                chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
                chkLoop.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

                if (!_midiPlayer.Valid)
                {
                    MessageBox.Show($"Invalid midi output device: {_settings.MidiDeviceName}");
                    _valid = false;
                }

                if (!_audioPlayer.Valid)
                {
                    MessageBox.Show($"Invalid audio output device: {_settings.WavOutDevice}");
                    _valid = false;
                }

                if (!_valid)
                {
                    // Disable everything except settings button.
                    sldVolume.Enabled = false;
                    chkPlay.Enabled = false;
                    chkLoop.Enabled = false;
                    btnRewind.Enabled = false;
                    cmbDrumChannel.Enabled = false;

                    btnSettings.Click += Settings_Click;
                }
                else
                {
                    // Hook up UI handlers.
                    chkPlay.CheckedChanged += (_, __) => { _ = chkPlay.Checked ? _player.Play() : _player.Stop(); };
                    btnRewind.Click += (_, __) => { _player.Rewind(); progress.AddValue(0); };
                    sldVolume.ValueChanged += (_, __) => { _player.Volume = sldVolume.Value; };
                    btnSettings.Click += Settings_Click;
                    progress!.MouseDown += Progress_MouseDown;
                    progress!.MouseMove += Progress_MouseMove;

                    // Drum channel selection.
                    cmbDrumChannel.BackColor = _settings.SelectedColor;
                    for (int i = 0; i < MidiDefs.MAX_CHANNEL; i++)
                    {
                        cmbDrumChannel.Items.Add($"{i + 1}");
                    }
                    cmbDrumChannel.SelectedIndexChanged += (_, __) => { _midiPlayer?.DrumChannel = cmbDrumChannel.SelectedIndex + 1; };
                    cmbDrumChannel.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL - 1;
                }

                if (!_settings.Debug)
                {
                    ClientSize = new(ClientSize.Width, rtbLog.Top);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ClipPlayer failed: {ex}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Form is legal now. Init things that want to log.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            bool ok = true;

            if (_valid)
            {
                // Go!
                ok = OpenFile();
            }

            if (ok)
            {
                // Start listening for new app instances.
                _server = new Ipc.Server(Common.PipeName, Common.LogFileName);
                _server.Receive += Server_IpcReceive;
                _server.Start();
            }
            else
            {
                // Bail out
                Environment.ExitCode = 1;
                Close();
            }
            base.OnLoad(e);
        }

        /// <summary>
        /// Goodbye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _settings.FormGeometry = new(Location, Size);
            _settings.Save();
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

            _midiPlayer?.Stop();
            _midiPlayer?.Dispose();

            _audioPlayer?.Stop();
            _audioPlayer?.Dispose();

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
                        if (_midiPlayer is null || !_midiPlayer.Valid)
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
                        if (_audioPlayer is null || !_audioPlayer.Valid)
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

                if (ok)
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
                MessageBox.Show($"Failed to open file: {ex.Message}");
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
        void Player_ProgressChange(object? sender, ProgressChangeEventArgs e)
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
                        if (_settings.AutoClose)
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
        void Server_IpcReceive(object? sender, Ipc.ReceiveEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if (e.Error)
                {
                    _logger.Warn($"Server error:{e.Message}");
                }
                else
                {
                    _fn = e.Message;
                    bool ok = OpenFile();
                    if (!ok)
                    {
                        // Bail out
                        Environment.ExitCode = 1;
                        Close();
                    }
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
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "WavOutDevice":
                    case "Latency":
                    case "MidiDeviceName":
                    case "DrawColor":
                    case "SelectedColor":
                        restart = true;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            _settings.Save();
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
