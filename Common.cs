using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipPlayer
{
    public enum RunState { Stopped, Runnning, Complete, Error }

    public class StatusEventArgs : EventArgs
    {
        /// <summary>0 -> 100</summary>
        public int Progress { get; set; } = 0;
        /// <summary>Where we at.</summary>
        public RunState State { get; set; } = RunState.Stopped;
        /// <summary>Optional info.</summary>
        public string Message { get; set; } = "";
    }

    interface IPlayer : IDisposable
    {
        /// <summary>Something changed.</summary>
        event EventHandler<StatusEventArgs> StatusEvent;

        /// <summary>Open playback file in player.</summary>
        bool OpenFile(string fn);

        /// <summary>Start playback.</summary>
        void Start();

        /// <summary>Stop playback.</summary>
        void Stop();

        /// <summary>Return to beginning.</summary>
        void Rewind();
    }

    public class Common
    {
        /// <summary>Common volume setting.</summary>
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
    }
}
