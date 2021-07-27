using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;


//DOCDOCDOC simple uni-directional ipc. for a way to send a string from a client to a server.

namespace ClipPlayer // TODO put in nbot?
{

    public enum IpcStatus { None, LogMessage, RcvMessage, ServerError, ClientError, ClientTimeout }

    public class IpcEventArgs : EventArgs
    {
        public IpcStatus Status { get; set; } = IpcStatus.None;
        public string Message { get; set; } = "";
    }

    public class IpcServer : IDisposable
    {
        /// <summary>Named pipe name.</summary>
        string _pipeName;

        /// <summary>The server thread.</summary>
        Thread _thread = null;

        /// <summary>Flag to unblock the listen and end the thread.</summary>
        bool _running = true;

        /// <summary>Something changed event. Client will have to take care of thread issues.</summary>
        public event EventHandler<IpcEventArgs> IpcEvent = null;

        /// <summary>The canceller.</summary>
        ManualResetEvent _cancelEvent = new ManualResetEvent(false);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName"></param>
        public IpcServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        /// <summary>
        /// Run it.
        /// </summary>
        public void Start()
        {
            _thread = new Thread(ServerThread);
            _thread.Start();
        }

        /// <summary>
        /// Kill the server.
        /// </summary>
        /// <returns></returns>
        public bool Kill()
        {
            bool ok = true;

            Common.Dump($"SERVER Kill...");

            _running = false;
            _cancelEvent.Set();

            Common.Dump($"SERVER Shutting down");
            _thread.Join();
            Common.Dump("SERVER Thread shut down");
            _thread = null;

            return ok;
        }

        /// <summary>
        /// Required.
        /// </summary>
        public void Dispose()
        {
            _cancelEvent.Dispose();
        }

        /// <summary>
        /// Listen for client messages.
        /// </summary>
        void ServerThread()
        {
            var buffer = new byte[1024];
            var index = 0;

            Common.Dump($"SERVER thread started");

            using (var stream = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                while (_running)
                {
                    Exception e = null;

                    try
                    {
                        AutoResetEvent connectEvent = new AutoResetEvent(false); //TODO using

                        stream.BeginWaitForConnection(ar =>
                        {
                            Common.Dump($"SERVER BeginWaitForConnection");

                            //TODO: The "right" way to do an interruptible WaitForConnection is to call BeginWaitForConnection, handle the
                            //new connection in the callback, and close the pipe stream to stop waiting for connections.
                            //If the pipe is closed, EndWaitForConnection will throw ObjectDisposedException which the callback
                            //thread can catch, clean up any loose ends, and exit cleanly.


                            stream.EndWaitForConnection(ar);

                            //////////////////////////////////
                            // A client wants to tell us something.
                            Common.Dump($"SERVER client connected");

                            var bytesRead = stream.Read(buffer, index, buffer.Length - index);

                            Common.Dump($"SERVER read:{bytesRead}");

                            if (bytesRead > 0) //TODO need a timeout.
                            {
                                index += bytesRead;

                                // Full string arrived?
                                int terminator = Array.IndexOf(buffer, (byte)'\n');

                                if (terminator >= 0)
                                {
                                    // Make buffer into a string.
                                    string msg = new UTF8Encoding().GetString(buffer, 0, terminator);

                                    Common.Dump($"SERVER got fn:{msg}");

                                    // Process the line.
                                    IpcEvent?.Invoke(this, new IpcEventArgs() { Message = msg, Status = IpcStatus.RcvMessage });

                                    // Reset buffer.
                                    index = 0;
                                }
                            }

                            // Signal happy completion.
                            connectEvent.Set();

                        }, null);//BeginWaitForConnection

                        int sig = WaitHandle.WaitAny(new WaitHandle[] { connectEvent, _cancelEvent });

                        if (sig == 1)
                        {
                            Common.Dump($"SERVER shutdown sig");
                            _running = false;
                        }

                        connectEvent.Dispose();

                    }
                    catch (Exception er)
                    {
                        e = er;
                    }

                    if (e != null)
                    {
                        //throw e; // TODO rethrow exception
                    }
                }
            }

            Common.Dump($"SERVER thread ended");
        }
    }

    /// <summary>Companion client to server.</summary>
    public class IpcClient
    {
        /// <summary></summary>
        readonly string _pipeName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName"></param>
        public IpcClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        /// <summary>
        /// Blocking send string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IpcStatus Send(string s, int timeout)
        {
            IpcStatus res = IpcStatus.None;

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out))
                {
                    Common.Dump($"CLIENT 1 s:{s}");

                    pipeClient.Connect(timeout);

                    Common.Dump($"CLIENT 2");

                    byte[] outBuffer = new UTF8Encoding().GetBytes(s + "\n");

                    Common.Dump($"CLIENT 3");

                    pipeClient.Write(outBuffer, 0, outBuffer.Length);

                    Common.Dump($"CLIENT 4");

                    //pipeClient.Flush();
                    pipeClient.WaitForPipeDrain();

                    Common.Dump($"CLIENT 5");

                    // Then exit.
                }
            }
            catch (TimeoutException)//TODO handle?
            {
                Common.Dump($"CLIENT timed out");
                res = IpcStatus.ClientTimeout;
            }
            catch (IOException ex)//TODO handle?
            {
                Common.Dump($"CLIENT {ex}");
                res = IpcStatus.ClientError;
            }
            catch (Exception ex)
            {
                Common.Dump($"CLIENT !!!!! {ex}");
                res = IpcStatus.ClientError;
            }

            return res;
        }
    }
}
