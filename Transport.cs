using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NBagOfTricks;


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

        /// <summary>Watch for changes to sem file.</summary>
        FileSystemWatcher _watcher;
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
            if (Environment.GetCommandLineArgs().Length != 2)
            {
                // Should never happen.
                Environment.Exit(1);
            }

            Icon = Properties.Resources.croco;
            bool ok = true;

            var fn = Environment.GetCommandLineArgs()[1];

            Log($"CurrentDirectory:{Environment.CurrentDirectory}");
            Log($"ExecutablePath:{Application.ExecutablePath}");
            Log($"StartupPath:{Application.StartupPath}");

            // Get the settings.
            string appDir = Common.GetAppDir();
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);
            sldVolume.Value = Common.Settings.Volume;
            Location = Common.Settings.Position;

            if(!Common.Settings.ShowLog)
            {
                ClientSize = new Size(ClientRectangle.Width, progress.Bottom + 5);
            }

            // Hook up UI handlers.
            pbPlay.Click += (_, __) => { _player.Play(); };
            pbStop.Click += (_, __) => { _player.Stop(); };
            pbRewind.Click += (_, __) => { _player.Rewind(); progress.AddValue(0); };
            sldVolume.ValueChanged += (_, __) => { Common.Settings.Volume = sldVolume.Value; _player.Volume = sldVolume.Value; };

            // Hook up sem file watcher.
            var path = Common.GetSemFile();
            File.WriteAllText(path, "nothing-here-yet");
            _watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path),
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };
            _watcher.Changed += (_, __) =>
            {
                fn = File.ReadAllText(Common.GetSemFile());
                OpenFile(fn);
            };

            ok = OpenFile(fn);

            if(!ok)
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

            RemovePlayer();

            base.Dispose(disposing);
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
        /// Open a file to play.
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        bool OpenFile(string fn)
        {
            bool ok = true;

            try
            {
                // Remove old one.
                RemovePlayer();

                switch (Path.GetExtension(fn).ToLower())
                {
                    case ".mid":
                        _player = new MidiPlayer();
                        break;

                    case ".wav":
                    case ".mp3":
                    case ".m4a":
                    case ".flac":
                        _player = new WavePlayer();
                        break;

                    default:
                        ShowMessage($"Invalid file: {fn}", true);
                        ok = false;
                        break;
                }

                if (_player != null)
                {
                    _player.StatusEvent += Player_StatusEvent;
                    _player.Volume = sldVolume.Value;

                    if (_player.OpenFile(fn))
                    {
                        Text = $"{Path.GetFileName(fn)} {_player.GetInfo()}";
                        _fn = fn;
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

            if(_fn != "")
            {
                Log($"File to play:{_fn}");
            }

            return ok;
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

        /// <summary>
        /// Debug help.
        /// </summary>
        /// <param name="s"></param>
        void Log(string s)
        {
            logBox.AppendText(s + Environment.NewLine);
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        void RemovePlayer()
        {
            if (_player != null)
            {
                _player.Stop();
                _player.StatusEvent -= Player_StatusEvent;
                _player.Dispose();
                _player = null;
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
            if(e.Message != "")
            {
                Log(e.Message);
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
                    if(Common.Settings.AutoClose)
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
