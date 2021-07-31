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
    /// This is not intended to be a general purpose logger but one that serves a specific purpose,
    /// to debug the SimpleIpc component.
    /// </summary>
    public class MpLog
    {
        /// <summary>File lock id.</summary>
        const string MUTEX_GUID = "65A7B2CE-D1A1-410F-AA57-1146E9B29E02";

        /// <summary>Which file.</summary>
        string _filename;

        /// <summary>For sorting.</summary>
        string _category = "????";

        /// <summary>Rollover size.</summary>
        int _maxSize = 10000;

        /// <summary>
        /// Init the log file.
        /// </summary>
        /// <param name="filename">The file.</param>
        /// <param name="filename">The category.</param>
        public MpLog(string filename, string category)
        {
            _filename = filename;
            int catSize = 4;
            _category = category.Length >= catSize ? category.Left(catSize) : category.PadRight(catSize);

            // Good time to check file size.
            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                mutex.WaitOne();
                FileInfo fi = new FileInfo(_filename);
                if(fi.Exists && fi.Length > _maxSize)
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
        /// <param name="s"></param>
        /// <param name="error">T/F</param>
        public void Write(string s, bool error = false)
        {
            var se = error ? "ERROR!!" : "";
            s = $"{DateTime.Now.ToString(Common.TS_FORMAT)} {_category} {se} {Process.GetCurrentProcess().Id, 5} {Thread.CurrentThread.ManagedThreadId, 2} {s}{Environment.NewLine}";

            using (var mutex = new Mutex(false, MUTEX_GUID))
            {
                mutex.WaitOne();
                File.AppendAllText(_filename, s);
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Empty the log file.
        /// </summary>
        public void Clear()
        {
            File.WriteAllText(_filename, "");
        }
    }
}
