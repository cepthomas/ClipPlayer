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

// Simple IPC mechanism so there is only one instance but can be updated from a new one. TOCO put in nbot?

namespace ClipPlayer
{
    /// <summary>Possible outcomes.</summary>
    public enum IpcServerStatus { Ok, Message, Error }

    /// <summary>Possible outcomes.</summary>
    public enum IpcClientStatus { Ok, Timeout, Error }

    public class IpcServerEventArgs : EventArgs
    {
        public IpcServerStatus Status { get; set; } = IpcServerStatus.Ok;
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

        /// <summary>Something happened. Client will have to take care of thread issues.</summary>
        public event EventHandler<IpcServerEventArgs> IpcServerEvent = null;

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

            TraceLog.Trace("SERVER", $"Kill()");

            _running = false;
            _cancelEvent.Set();

            TraceLog.Trace("SERVER", $"Shutting down");
            _thread.Join();
            TraceLog.Trace("SERVER", $"Thread ended");
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
        /// Listen for client messages. Interruptible by setting _cancelEvent.
        /// </summary>
        void ServerThread()
        {
            var buffer = new byte[1024];
            var index = 0;

            TraceLog.Trace("SERVER", $"thread started");

            while (_running)
            {
                using (var stream = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                using (AutoResetEvent connectEvent = new AutoResetEvent(false))
                {
                    Exception et = null;

                    try
                    {
                        TraceLog.Trace("SERVER", $"before BeginWaitForConnection");
                        stream.BeginWaitForConnection(ar =>
                        {
                            try
                            {
                                // This is running in a new thread.
                                TraceLog.Trace("SERVER", $"before EndWaitForConnection");
                                stream.EndWaitForConnection(ar);
                                TraceLog.Trace("SERVER", $"after EndWaitForConnection - client connected");

                                // A client wants to tell us something.
                                var numRead = stream.Read(buffer, index, buffer.Length - index);

                                TraceLog.Trace("SERVER", $"num read:{numRead}");

                                if (numRead > 0) //TODO need a timeout.
                                {
                                    index += numRead;

                                    // Full string arrived?
                                    int terminator = Array.IndexOf(buffer, (byte)'\n');

                                    if (terminator >= 0)
                                    {
                                        // Make buffer into a string.
                                        string msg = new UTF8Encoding().GetString(buffer, 0, terminator);

                                        TraceLog.Trace("SERVER", $"got msg:{msg}");

                                        // Process the line.
                                        IpcServerEvent?.Invoke(this, new IpcServerEventArgs() { Message = msg, Status = IpcServerStatus.Message });

                                        // Reset buffer.
                                        index = 0;
                                    }
                                }
                            }
                            catch (Exception er)
                            {
                                // Pass any exceptions back to the main thread for handling.
                                et = er;
                            }

                            // Signal completion.
                            connectEvent.Set();
                        }, null);
                    }
                    catch (Exception ee)
                    {
                        et = ee;
                    }

                    // Wait for events of interest.
                    int sig = WaitHandle.WaitAny(new WaitHandle[] { connectEvent, _cancelEvent });

                    if (sig == 1)
                    {
                        TraceLog.Trace("SERVER", $"shutdown sig");
                        _running = false;
                    }
                    else if (et != null)
                    {
                        TraceLog.Trace("SERVER", $"exception:{et}");
                        throw et; // TODO rethrow exception?
                    }
                    // else done with this stream.
                }
            }

            TraceLog.Trace("SERVER", $"thread ended");
        }
    }

    /// <summary>Companion client to server. This runs in a new process.</summary>
    public class IpcClient
    {
        /// <summary>Pipe name.</summary>
        readonly string _pipeName;

        /// <summary>Caller may be able to use this.</summary>
        public string Error { get; set; } = "";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Pipe name to use.</param>
        public IpcClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        /// <summary>
        /// Blocking send string.
        /// </summary>
        /// <param name="s">String to send.</param>
        /// <param name="timeout">Msec to wait for completion.</param>
        /// <returns></returns>
        public IpcClientStatus Send(string s, int timeout)
        {
            IpcClientStatus res = IpcClientStatus.Ok;

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out))
                {
                    TraceLog.Trace("CLIENT", $"1 s:{s}");
                    pipeClient.Connect(timeout);

                    TraceLog.Trace("CLIENT", $"2");
                    byte[] outBuffer = new UTF8Encoding().GetBytes(s + "\n");

                    TraceLog.Trace("CLIENT", $"3");
                    pipeClient.Write(outBuffer, 0, outBuffer.Length);

                    TraceLog.Trace("CLIENT", $"4");
                    pipeClient.WaitForPipeDrain();

                    TraceLog.Trace("CLIENT", $"5");
                    // Now exit.
                }
            }
            catch (TimeoutException)
            {
                // Client can deal with this.
                TraceLog.Trace("CLIENT", $"timed out");
                res = IpcClientStatus.Timeout;
            }
            catch (Exception ex)
            {
                TraceLog.Trace("CLIENT", $"!!!!! {ex}");
                Error = ex.ToString();
                res = IpcClientStatus.Error;
            }

            return res;
        }
    }
}
