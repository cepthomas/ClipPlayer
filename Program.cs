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

            Console.WriteLine($"==========");
            Console.WriteLine($"========== {args.Length}");
            //Console.WriteLine($"{args[0]}:{args[1]}");

            // Ensure only one playing at a time. If this is the second, alert the primary by writing the
            // args to the semaphore file.
            if (procs.Length > 1)
            {
                File.WriteAllText(Common.GetSemFile(), args[0]);
                // Then exit.
            }
            else
            {
                // I'm the first, start normally.
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Transport());
            }
        }
    }
}
