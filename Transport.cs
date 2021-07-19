using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Midi;
using NAudio.Wave;
using NBagOfTricks;
using NBagOfTricks.UI;

//>>>>>>>>>>>>> TODO volume ui

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
            bool ok = true;

            if (Environment.GetCommandLineArgs().Length != 2)
            {
                // Should never happen.
                Environment.Exit(1);
            }

            _fn = Environment.GetCommandLineArgs()[1];

            Log($"File to play:{_fn}");
            Log($"CurrentDirectory:{Environment.CurrentDirectory}");
            Log($"ExecutablePath:{Application.ExecutablePath}");
            Log($"StartupPath:{Application.StartupPath}");

            // Get the settings.
            string appDir = MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera");
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);

            try
            {
                switch (Path.GetExtension(_fn).ToLower())
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
                        ShowMessage($"Invalid file: {_fn}", true);
                        ok = false;
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
                        ShowMessage($"Couldn't open file", true);
                        ok = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Fail: {ex}", true);
                ok = false;
            }

            if(!ok)
            {
                // Bail out.
                Environment.Exit(Environment.ExitCode);
            }
        }
        #endregion

        #region User settings
        /// <summary>
        /// Collect and save user settings.
        /// </summary>
        void SaveSettings()
        {
//TODO            Common.Settings.Volume = sldVolume.Value;
            Common.Settings.Position = Location;

            Common.Settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "User Settings",
                Size = new Size(450, 450),
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

                SaveSettings();
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

        /// <summary>
        /// Debug help.
        /// </summary>
        /// <param name="s"></param>
        void Log(string s)
        {
            logBox.AppendText(s + Environment.NewLine);
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
