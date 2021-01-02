using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NBagOfTricks.Utils;


namespace ClipPlayer
{
    public partial class WavePlayer : IPlayer
    {
        #region Fields
        /// <summary>Wave output play device.</summary>
        WaveOut _waveOut = null;

        /// <summary>Input device for playing wav file.</summary>
        AudioFileReader _audioFileReader = null;

        /// <summary>Stream read chunk.</summary>
        const int READ_BUFF_SIZE = 1000000;

        /// <summary>Total length.</summary>
        TimeSpan _length = TimeSpan.Zero;

        /// <summary>First valid point.</summary>
        TimeSpan _start = TimeSpan.Zero;

        /// <summary>Last valid point.</summary>
        TimeSpan _end = TimeSpan.Zero;

        /// <summary>Current.</summary>
        TimeSpan _current = TimeSpan.Zero;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler PlaybackCompleted;

        /// <inheritdoc />
        public event EventHandler<string> Log;
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
            // My stuff here.
            CloseAudio();
        }
        #endregion

        #region Public Functions - interface implementation
        /// <inheritdoc />
        public bool OpenFile(string fn)
        {
            bool ok = true;
            LogMessage($"Open:{fn}");

            // Clean up first.
            CloseAudio();

            // Create output device.
            for (int id = -1; id < WaveOut.DeviceCount; id++)
            {
                var cap = WaveOut.GetCapabilities(id);
                if (Common.WavOutDevice == cap.ProductName)
                {
                    _waveOut = new WaveOut
                    {
                        DeviceNumber = id,
                        DesiredLatency = Common.Latency
                    };
                    _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
                    break;
                }
            }

            // Create input device.
            if (_waveOut != null)
            {
                _audioFileReader = new AudioFileReader(fn);

                _length = _audioFileReader.TotalTime;
                _start = TimeSpan.Zero;
                _end = TimeSpan.Zero;
                _current = TimeSpan.Zero;

                // Create reader.
                var sampleChannel = new SampleChannel(_audioFileReader, false);
                sampleChannel.PreVolumeMeter += SampleChannel_PreVolumeMeter;
                var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
                //postVolumeMeter.StreamVolume += PostVolumeMeter_StreamVolume;
                _waveOut.Init(postVolumeMeter);
                _waveOut.Volume = Common.Volume;
            }
            else
            {
                ok = false;
            }

            if (!ok)
            {
                CloseAudio();
            }

            return ok;
        }

        /// <inheritdoc />
        public void Start()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                _waveOut.Play();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                _waveOut.Pause(); // or Stop?
            }
        }

        /// <inheritdoc />
        public void Rewind()
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                _audioFileReader.Position = 0;
                _current = TimeSpan.Zero;
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// 
        /// </summary>
        void CloseAudio()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }

        /// <summary>
        /// Logger.
        /// </summary>
        /// <param name="s"></param>
        void LogMessage(string s)
        {
            Log?.Invoke(this, s);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Usually end of file but could be error.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Log?.Invoke(this, e.Exception.Message);
            }

            PlaybackCompleted?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SampleChannel_PreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            _current = _audioFileReader.CurrentTime;
        }
        #endregion
    }
}
