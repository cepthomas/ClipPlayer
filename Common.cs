using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NBagOfTricks;


namespace ClipPlayer
{
    public enum RunState { Stopped, Playing, Complete }

    public class Common
    {
        /// <summary>Current global user settings.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();

        /// <summary>For viewing purposes.</summary>
        public const string TS_FORMAT = @"mm\:ss\.fff";

        /// <summary>Client/server comm id.</summary>
        public const string PIPE_NAME = "5826C396-B847-4F86-87A0-52475EDC0082";
    }

    /// <summary>
    /// A dumb logger which handles calls from multiple processes/threads.
    /// </summary>
    public class TraceLog
    {
        /// <summary>File lock id.</summary>
        const string MUTEX_GUID = "65A7B2CE-D1A1-410F-AA57-1146E9B29E02";

        /// <summary>Which.</summary>
        static string _filename = @"..\trace.txt";

        /// <summary>
        /// Cleans out the file.
        /// </summary>
        /// <param name="filename">The file.</param>
        /// <param name="clear">Clears out the file.</param>
        public static void Init(string filename, bool clear)
        {
            _filename = filename;

            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                mutex.WaitOne();
                string s = $"============================= {DateTime.Now} =============================";
                if(clear)
                {
                    File.WriteAllText(_filename, s);
                }
                else
                {
                    File.AppendAllText(_filename, s);
                }
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Add a line.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="s"></param>
        public static void Trace(string cat, string s)
        {
            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                s = $"{DateTime.Now.ToString(Common.TS_FORMAT)} {cat} {Process.GetCurrentProcess().Id} {Thread.CurrentThread.ManagedThreadId} {s}{Environment.NewLine}";
                mutex.WaitOne();
                File.AppendAllText(_filename, s);
                mutex.ReleaseMutex();
            }
        }
    }

    /// <summary>Player has something to say or show.</summary>
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

        /// <summary>Current volume.</summary>
        double Volume { get; set; }

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
