using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace ClipPlayer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                var fn = args[0];
                var proc = Process.GetCurrentProcess();
                var pname = proc.ProcessName;
                var procs = Process.GetProcessesByName(pname);

                // Ensure only one playing at a time.
                if (procs.Length == 1)
                {
                    // I'm the first, start normally by passing the file name.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Transport(fn));
                }
                else
                {
                    // If this is the second instance, alert the primary by connecting and sending the new file name.
                    var pipeClient = new NamedPipeClientStream(".", Common.PIPE_NAME, PipeDirection.Out);
                    pipeClient.Connect();
                    byte[] outBuffer = new UTF8Encoding().GetBytes(fn + "\n");
                    pipeClient.Write(outBuffer, 0, outBuffer.Length);
                    pipeClient.Flush();
                    pipeClient.Close();
                    // Then exit.
                }
            }
            else
            {
                MessageBox.Show("Missing file name argument");
            }
        }
    }
}
