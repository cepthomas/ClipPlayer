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
        /// <param name="fn">The file to open.</param>
        /// <returns>Success</returns>
        bool OpenFile(string fn);

        /// <summary>Start playback.</summary>
        void Start();

        /// <summary>Stop playback.</summary>
        void Stop();

        /// <summary>Stop playback and return to beginning.</summary>
        void Rewind();
    }

    public class Common
    {
        public static float Volume { get; set; } = 0.8f;

        public static bool LogEvents { get; set; } = false;

        public static string WavOutDevice { get; set; } = "Microsoft Sound Mapper";

        public static int Latency { get; set; } = 200;

        public static string MidiOutDevice { get; set; } = "";

        public static int DrumChannel { get; set; } = 0;

        public static int Tempo { get; set; } = 100;
    }
}
