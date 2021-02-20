using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;


namespace ClipPlayer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var proc = Process.GetCurrentProcess();
            var pname = proc.ProcessName;
            var procs = Process.GetProcessesByName(pname);

            if(procs.Length > 1)
            {
                // Kill any currently running - this will essentialy replace it.
                foreach(Process p in procs)
                {
                    if(p.Id != proc.Id)
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
