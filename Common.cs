using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipPlayer
{
    public enum RunState { Stopped, Playing, Complete, Error }

    public class StatusEventArgs : EventArgs
    {
        /// <summary>0 -> 100</summary>
        public int Progress { get; set; } = 0;

        /// <summary>Where we at.</summary>
        public string Message { get; set; } = "";
    }

    interface IPlayer : IDisposable
    {
        /// <summary>What are we doing right now.</summary>
        RunState State { get; set; }

        ///// <summary>Where we at in file.</summary>
        //TimeSpan Position { get; set; }

        /// <summary>Total length.</summary>
        TimeSpan Length { get; }

        /// <summary>Current time.</summary>
        TimeSpan Current { get; set; }

        /// <summary>Something changed event.</summary>
        event EventHandler<StatusEventArgs> StatusEvent;

        /// <summary>Open playback file in player.</summary>
        bool OpenFile(string fn);

        /// <summary>User friendly stuff.</summary>
        string GetInfo();

        /// <summary>Start playback.</summary>
        void Play();

        /// <summary>Stop playback.</summary>
        void Stop();

        /// <summary>Return to beginning.</summary>
        void Rewind();
    }

    public class Common
    {
        /// <summary>Common volume setting. Range is 0.0 to 1.0.</summary>
        public static float Volume { get; set; } = 0.8f;

        /// <summary>Wave device.</summary>
        public static string WavOutDevice { get; set; } = "Microsoft Sound Mapper";

        /// <summary>Wave performance.</summary>
        public static int Latency { get; set; } = 200;

        /// <summary>Midi device.</summary>
        public static string MidiOutDevice { get; set; } = "Microsoft GS Wavetable Synth";

        /// <summary>Some midi files have drums on non-standard channel.</summary>
        public static int DrumChannel { get; set; } = 0;

        /// <summary>Midi files may or may not specify this in the file.</summary>
        public static int Tempo { get; set; } = 100;

        /// <summary>For viewing purposes.</summary>
        public const string TS_FORMAT = @"mm\:ss\.fff";
    }
}
