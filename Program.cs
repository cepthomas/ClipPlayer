using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace ClipPlayer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var fn = args[0];
                var proc = Process.GetCurrentProcess();
                var pname = proc.ProcessName;
                var procs = Process.GetProcessesByName(pname);

                Common.Dump($"==============================================");
                Common.Dump($"MAIN num:{procs.Length} pid:{proc.Id} fn:{fn}");

                // Ensure only one playing at a time.
                if (procs.Length == 1)
                {
                    Common.Dump($"MAIN one enter");

                    // I'm the first, start normally by passing the file name.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Transport(fn));
                    Common.Dump($"MAIN one exit");
                }
                else
                {
                    // If this is the second instance, alert the primary by connecting and sending the new file name.
                    Common.Dump($"MAIN two enter");

                    IpcClient client = new IpcClient(Common.PIPE_NAME);
                    var res = client.Send(fn, 1000);

                    Common.Dump($"MAIN two exit {res}");
                }
            }
            else
            {
                MessageBox.Show("Missing file name argument");
            }
        }
    }
}
