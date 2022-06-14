﻿using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NBagOfTricks;
using NBagOfTricks.Slog;
using AudioLib;


namespace ClipPlayer
{
    public sealed class AudioClipPlayer : IPlayer
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("AudioClipPlayer");

        /// <summary>Wave output play device.</summary>
        readonly AudioPlayer _player;

        /// <summary>Input device for playing wav file.</summary>
        AudioFileReader? _audioFileReader = null;

        /// <summary>Current volume.</summary>
        double _volume = 0.5;
        #endregion

        #region Properties - interface implementation
        /// <inheritdoc />
        public RunState State { get; set; } = RunState.Stopped;

        /// <inheritdoc />
        public TimeSpan Length { get; private set; } = TimeSpan.Zero;

        /// <summary>Current time.</summary>
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
        public event EventHandler<StatusEventArgs>? StatusEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public AudioClipPlayer()
        {
            // Create output device.
            _player = new(Common.Settings.WavOutDevice, int.Parse(Common.Settings.Latency));
            _player.PlaybackStopped += Player_PlaybackStopped;
        }

        /// <summary>
        /// Usually end of file but could be error.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception is not null)
            {
                _logger.Exception(e.Exception, "Other NAudio error");
            }

            DoUpdate();
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

            // Create input device.
            _audioFileReader?.Dispose(); //old one
            _audioFileReader = new AudioFileReader(fn);
            Length = _audioFileReader.TotalTime;

            // Create reader.
            var sampleChannel = new SampleChannel(_audioFileReader, false);
            sampleChannel.PreVolumeMeter += SampleChannel_PreVolumeMeter;

            var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
            //postVolumeMeter.StreamVolume += PostVolumeMeter_StreamVolume;

            _player.Init(postVolumeMeter);
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
        public void SettingsChanged()
        {
            // nada
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Tell the mothership.
        /// </summary>
        void DoUpdate()
        {
            int prog;
            if(Current < Length)
            {
                prog = 100 * (int)Current.TotalMilliseconds / (int)Length.TotalMilliseconds;

            }
            else
            {
                prog = 100;
                State = RunState.Complete;
            }

            StatusEvent?.Invoke(this, new StatusEventArgs()
            {
                Progress = prog
            });
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Usually end of file but could be error. Also when client tells it to Stop();
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception is not null)
            {
                _logger.Exception(e.Exception, "Bad thing");
            }
            else
            {
                State = RunState.Complete;
                DoUpdate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SampleChannel_PreVolumeMeter(object? sender, StreamVolumeEventArgs e)
        {
            DoUpdate();
        }
        #endregion
    }
}
