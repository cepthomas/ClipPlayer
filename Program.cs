using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;


namespace ClipPlayer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var proc = Process.GetCurrentProcess();
            var pname = proc.ProcessName;
            var procs = Process.GetProcessesByName(pname);

            // Ensure only one playing at a time. TODO would be nicer to restart this one with a new file.
            if (procs.Length > 1)
            {
                // Kill any currently running - this one will essentially replace it.
                foreach (Process p in procs)
                {
                    if (p.Id != proc.Id)
                    {
                        p.Kill();
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Transport());
        }
    }
}
