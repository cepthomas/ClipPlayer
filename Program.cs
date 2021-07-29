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

                string tracefn = NBagOfTricks.MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera") + @"\mplog.txt";
                MpLog.Init(tracefn);
                MpLog.Write("MAIN", $"num-procs:{procs.Length} pid:{proc.Id} arg-fn:{fn}");

                // Ensure only one playing at a time.
                if (procs.Length == 1)
                {
                    MpLog.Write("MAIN", $"===============================================================================");
                    MpLog.Write("MAIN", $"main thread enter");

                    // I'm the first, start normally by passing the file name.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Transport(fn));
                    MpLog.Write("MAIN", $"main thread exit");
                }
                else
                {
                    // If this is the second instance, alert the primary by connecting and sending the new file name.
                    MpLog.Write("MAIN", $"sub thread enter");

                    IpcClient client = new IpcClient(Common.PIPE_NAME);
                    var res = client.Send(fn, 1000);

                    switch (res)
                    {
                        case IpcClientStatus.Error:
                            MpLog.Write("MAIN", $"Client error:{client.Error}");
                            MessageBox.Show(client.Error, "Error!");
                            break;

                        case IpcClientStatus.Timeout:
                            MpLog.Write("MAIN", $"Client timeout");
                            MessageBox.Show("Timeout!");
                            break;
                    }

                    MpLog.Write("MAIN", $"sub thread exit {res}");
                }
            }
            else
            {
                MessageBox.Show("Missing file name argument");
            }
        }
    }
}
