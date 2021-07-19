using System;
using System.Collections.Generic;
using System.Text;

namespace ClipPlayer
{
    public enum RunState { Stopped, Playing, Complete, Error }

    public class Common
    {
        /// <summary>Current global user settings.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();

        /// <summary>For viewing purposes.</summary>
        public const string TS_FORMAT = @"mm\:ss\.fff";
    }

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
}
