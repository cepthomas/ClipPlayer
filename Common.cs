using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipPlayer
{
    interface IPlayer : IDisposable
    {
        /// <summary>Client needs to know when playing is done.</summary>
        event EventHandler PlaybackCompleted;

        /// <summary>Log me please.</summary>
        event EventHandler<string> Log;

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

        public static int Latency { get; set; } = 200;

        /// <summary>Midi device.</summary>
        public static string MidiOutDevice { get; set; } = "Microsoft GS Wavetable Synth";

        /// <summary>Some midi files have drums on non-standard channel.</summary>
        public static int DrumChannel { get; set; } = 0;

        /// <summary>Midi files may or may not specify this in the file.</summary>
        public static int Tempo { get; set; } = 100;
    }
}
