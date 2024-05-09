using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.AudioLib;


namespace ClipPlayer
{
    sealed class AudioClipPlayer : IPlayer
    {
        #region Fields
        /// <summary>Wave output play device.</summary>
        readonly AudioPlayer _player;

        /// <summary>Input device for audio player.</summary>
        readonly SwappableSampleProvider _waveOutSwapper;

        /// <summary>Input device for playing wav file.</summary>
        AudioFileReader? _audioFileReader = null;

        /// <summary>Current volume.</summary>
        double _volume = 0.5;

        /// <summary>Clean up if resampled.</summary>
        const string RESAMPLE_FILE = "resampled_file_delete_me.wav";
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get; private set; } = TimeSpan.Zero;

        /// <inheritdoc />
        public TimeSpan Current
        {
            get { return _audioFileReader is null ? new() : _audioFileReader.CurrentTime; }
            set { if (_audioFileReader is not null) _audioFileReader.CurrentTime = value < Length ? value : Length; }
        }

        /// <inheritdoc />
        public bool Valid { get { return _player.Valid; } }

        /// <inheritdoc />
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, 1); _player.Volume = (float)_volume; }
        }
        #endregion

        #region Events - interface implementation
        /// <inheritdoc />
        public event EventHandler<StatusChangeEventArgs>? StatusChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public AudioClipPlayer()
        {
            // Create output device.
            _waveOutSwapper = new();
            _player = new(Common.Settings.AudioSettings.WavOutDevice, int.Parse(Common.Settings.AudioSettings.Latency), _waveOutSwapper);
            _player.PlaybackStopped += Player_PlaybackStopped;
        }

        /// <summary>
        /// Shutdown.
        /// </summary>
        public void Dispose()
        {
            _player.Dispose();

            _audioFileReader?.Dispose();
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            bool ok = true;

            // Clean up first.
            _audioFileReader?.Dispose();
            File.Delete(RESAMPLE_FILE);

            // Create input device.
            _audioFileReader = new AudioFileReader(fn);

            // If it doesn't match, create a resampled temp file. Display original filename.
            if (_audioFileReader.WaveFormat.SampleRate != AudioLibDefs.SAMPLE_RATE)
            {
                _audioFileReader.Dispose();
                NAudioEx.Resample(fn, RESAMPLE_FILE);
                _audioFileReader = new AudioFileReader(RESAMPLE_FILE);
            }
            Length = _audioFileReader.TotalTime;

            // Create reader.
            var sampleChannel = new SampleChannel(_audioFileReader, false);
            sampleChannel.PreVolumeMeter += SampleChannel_PreVolumeMeter;

            var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
            //postVolumeMeter.StreamVolume += PostVolumeMeter_StreamVolume;

            _waveOutSwapper.SetInput(postVolumeMeter);
            _audioFileReader.Position = 0; // rewind
            _player.Volume = (float)Common.Settings.Volume;

            return ok;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            return Length.ToString(@"mm\:ss\.fff");
        }

        /// <inheritdoc />
        public RunState Play()
        {
            if (_audioFileReader is not null && _player.Valid)
            {
                _player.Run(true);
                if (_player.Playing)
                {
                    State = RunState.Playing;
                }
            }
            return State;
        }

        /// <inheritdoc />
        public RunState Stop()
        {
            if (_audioFileReader is not null && _player.Valid)
            {
                _player.Run(false);
                State = RunState.Stopped;
            }
            return State;
        }

        /// <inheritdoc />
        public void Rewind()
        {
            Current = TimeSpan.Zero;
        }

        /// <inheritdoc />
        public void UpdateSettings()
        {
            // nada
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Usually end of file but could be error.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            var evt = new StatusChangeEventArgs() { Progress = 100 };
            State = RunState.Complete;

            if (e.Exception is not null)
            {
                evt.Error = e.Exception.Message;
                evt.Progress = 0;
            }

            StatusChange?.Invoke(this, evt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SampleChannel_PreVolumeMeter(object? sender, StreamVolumeEventArgs e)
        {
            int prog = 100 * (int)Current.TotalMilliseconds / (int)Length.TotalMilliseconds;
            StatusChange?.Invoke(this, new StatusChangeEventArgs()
            {
                Progress = prog
            });
        }
        #endregion
    }
}
