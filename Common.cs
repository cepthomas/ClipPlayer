using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace Ephemera.ClipPlayer
{
    public enum RunState { Stopped, Playing, Complete }

    public class Common
    {
        /// <summary>Current global user settings.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();

        /// <summary>Client/server comm id.</summary>
        public static string PipeName { get { return "5826C396-B847-4F86-87A0-52475EDC0082"; } }

        /// <summary>Shared log file.</summary>
        public static string LogFileName { get { return MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera") + @"\mplog.txt"; } }
    }

    /// <summary>Player has something to say or show.</summary>
    public class StatusChangeEventArgs : EventArgs
    {
        /// <summary>0 -> 100</summary>
        public int Progress { get; set; } = 0;

        ///// <summary>Where we at.</summary>
        //public string Message { get; set; } = "";
    }

    interface IPlayer : IDisposable
    {
        /// <summary>What are we doing right now.</summary>
        RunState State { get; set; }

        /// <summary>Total length.</summary>
        TimeSpan Length { get; }

        /// <summary>Current time.</summary>
        TimeSpan Current { get; set; }

        /// <summary>Current volume.</summary>
        double Volume { get; set; }

        /// <summary>Are we good to go.</summary>
        bool Valid { get; }

        /// <summary>Something changed event.</summary>
        event EventHandler<StatusChangeEventArgs> StatusChange;

        /// <summary>Open playback file in player.</summary>
        bool OpenFile(string fn);

        /// <summary>User friendly stuff.</summary>
        string GetInfo();

        /// <summary>Start playback.</summary>
        RunState Play();

        /// <summary>Stop playback.</summary>
        RunState Stop();

        /// <summary>Return to beginning.</summary>
        void Rewind();

        /// <summary>Return to beginning.</summary>
        void UpdateSettings();
    }
}
