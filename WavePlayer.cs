using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NBagOfTricks;


namespace ClipPlayer
{
    public class WavePlayer : IPlayer
    {
        #region Fields
        /// <summary>Wave output play device.</summary>
        WaveOut _waveOut = null;

        /// <summary>Input device for playing wav file.</summary>
        AudioFileReader _audioFileReader = null;

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
            get { return _audioFileReader.CurrentTime; }
            set { _audioFileReader.CurrentTime = value < Length ? value : Length; }
        }

        /// <inheritdoc />
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, 1); if (_waveOut != null) _waveOut.Volume = (float)_volume; }
        }
        #endregion

        #region Events - interface implementation
        /// <inheritdoc />
        public event EventHandler<StatusEventArgs> StatusEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public WavePlayer()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            bool ok = true;

            // Create output device.
            for (int id = -1; id < WaveOut.DeviceCount; id++)
            {
                var cap = WaveOut.GetCapabilities(id);
                if (Common.Settings.WavOutDevice == cap.ProductName)
                {
                    _waveOut = new WaveOut
                    {
                        DeviceNumber = id,
                        DesiredLatency = int.Parse(Common.Settings.Latency)
                    };
                    _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
                    break;
                }
            }

            // Create input device.
            if (_waveOut != null)
            {
                _audioFileReader = new AudioFileReader(fn);

                Length = _audioFileReader.TotalTime;
                //_current = TimeSpan.Zero;

                // Create reader.
                var sampleChannel = new SampleChannel(_audioFileReader, false);
                sampleChannel.PreVolumeMeter += SampleChannel_PreVolumeMeter;
                var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
                //postVolumeMeter.StreamVolume += PostVolumeMeter_StreamVolume;
                _waveOut.Init(postVolumeMeter);
                _waveOut.Volume = (float)Common.Settings.Volume;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        /// <inheritdoc />
        public string GetInfo()
        {
            return Length.ToString(@"mm\:ss\.fff");
        }

        /// <inheritdoc />
        public void Play()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                _waveOut.Play();
                State = RunState.Playing;
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                _waveOut.Pause(); // or Stop?
                State = RunState.Stopped;
            }
        }

        /// <inheritdoc />
        public void Rewind()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                Current = TimeSpan.Zero;
            }
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
            StatusEvent.Invoke(this, new StatusEventArgs()
            {
                Progress = Current < Length ? 100 * (int)Current.TotalMilliseconds / (int)Length.TotalMilliseconds : 100
            });
        }

        /// <summary>
        /// Tell the mothership.
        /// </summary>
        /// <param name="msg"></param>
        void Tell(string msg)
        {
            StatusEvent.Invoke(this, new StatusEventArgs()
            {
                Progress = 0,
                Message = msg
            });
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Usually end of file but could be error. Also when client tells it to Stop();
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Tell(e.Exception.Message);
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
        void SampleChannel_PreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            DoUpdate();
        }
        #endregion
    }
}
