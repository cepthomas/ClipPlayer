using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NBagOfTricks;


namespace ClipPlayer
{
    /// <summary>
    /// A simple logger which handles client calls from multiple processes/threads.
    /// </summary>
    public class MpLog
    {
        /// <summary>File lock id.</summary>
        const string MUTEX_GUID = "65A7B2CE-D1A1-410F-AA57-1146E9B29E02";

        /// <summary>Which.</summary>
        static string _filename;

        /// <summary>
        /// Cleans out the file.
        /// </summary>
        /// <param name="filename">The file.</param>
        public static void Init(string filename)
        {
            _filename = filename;

            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                // Good time to check file size.
                mutex.WaitOne();
                FileInfo fi = new FileInfo(_filename);
                if(fi.Exists && fi.Length > 10000)
                {
                    string ext = fi.Extension;
                    File.Copy(fi.FullName, fi.FullName.Replace(ext, "_old" + ext), true);
                    Clear();
                }
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Add a line.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="s"></param>
        public static void Write(string cat, string s)
        {
            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                s = $"{DateTime.Now.ToString(Common.TS_FORMAT)} {cat} {Process.GetCurrentProcess().Id, 5} {Thread.CurrentThread.ManagedThreadId, 2} {s}{Environment.NewLine}";
                mutex.WaitOne();
                File.AppendAllText(_filename, s);
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Empty the log file.
        /// </summary>
        public static void Clear()
        {
            File.WriteAllText(_filename, "");
        }
    }
}
