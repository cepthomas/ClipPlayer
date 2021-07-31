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
            var _log = new MpLog(Common.LogFileName, "MAIN");

            if (args.Length > 0)
            {
                var fn = args[0];
                var proc = Process.GetCurrentProcess();
                var pname = proc.ProcessName;
                var procs = Process.GetProcessesByName(pname);

                _log.Write($"num-procs:{procs.Length} pid:{proc.Id} arg-fn:{fn}");

                // Ensure only one playing at a time.
                if (procs.Length == 1)
                {
                    _log.Write($"===============================================================================");
                    _log.Write($"main thread enter");

                    // I'm the first, start normally by passing the file name.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Transport(fn));
                    _log.Write($"main thread exit");
                }
                else
                {
                    // If this is the second instance, alert the primary by connecting and sending the new file name.
                    _log.Write($"sub thread enter");

                    IpcClient client = new IpcClient(Common.PIPE_NAME);
                    var res = client.Send(fn, 1000);

                    switch (res)
                    {
                        case IpcClientStatus.Error:
                            _log.Write($"Client error:{client.Error}", true);
                            MessageBox.Show(client.Error, "Error!");
                            break;

                        case IpcClientStatus.Timeout:
                            _log.Write($"Client timeout", true);
                            MessageBox.Show("Timeout!");
                            break;
                    }

                    _log.Write($"sub thread exit {res}");
                }
            }
            else
            {
                MessageBox.Show("Missing file name argument");
            }
        }
    }
}
