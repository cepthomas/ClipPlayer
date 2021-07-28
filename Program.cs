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

                string tracefn = NBagOfTricks.MiscUtils.GetAppDataDir("ClipPlayer", "Ephemera") + @"\trace.txt";

                TraceLog.Init(tracefn, false); //TODO not quite right...
                TraceLog.Trace("MAIN", $"num:{procs.Length} pid:{proc.Id} fn:{fn}");

                // Ensure only one playing at a time.
                if (procs.Length == 1)
                {
                    TraceLog.Trace("MAIN", $"thread-one enter");

                    // I'm the first, start normally by passing the file name.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Transport(fn));
                    TraceLog.Trace("MAIN", $"thread-one exit");
                }
                else
                {
                    // If this is the second instance, alert the primary by connecting and sending the new file name.
                    TraceLog.Trace("MAIN", $"two enter");

                    IpcClient client = new IpcClient(Common.PIPE_NAME);
                    var res = client.Send(fn, 1000);

                    switch (res)
                    {
                        case IpcClientStatus.Error:
                            MessageBox.Show(client.Error, "Error!");
                            break;

                        case IpcClientStatus.Timeout: //TODO handle?
                            MessageBox.Show("Timeout!");
                            break;
                    }

                    TraceLog.Trace("MAIN", $"thread-two exit {res}");
                }
            }
            else
            {
                MessageBox.Show("Missing file name argument");
            }
        }
    }
}
