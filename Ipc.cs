using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.IO.Pipes;
using Ephemera.NBagOfTricks;


namespace ClipPlayer.Ipc
{
    #region Server
    /// <summary>Possible states/outcomes.</summary>
    enum ConnectionStatus
    {
        Idle,           // Not connected, waiting.
        Receiving,      // Connected, collecting string.
        ValidMessage,   // Good message completed.
        Error           // Bad thing happened.
    }

    /// <summary>Per connection.</summary>
    class ConnectionState
    {
        public byte[] Buffer { get; set; } = new byte[1024];
        public int BufferIndex { get; set; } = 0;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Idle;
        public string Message { get; set; } = "";
    }

    /// <summary>Notify client of some connection event.</summary>
    public class ReceiveEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public bool Error { get; set; } = false;
    }

    public sealed class Server : IDisposable
    {
        /// <summary>Named pipe name.</summary>
        readonly string _pipeName;

        /// <summary>The server thread.</summary>
        Thread? _thread = null;

        /// <summary>Flag to unblock the listen and end the thread.</summary>
        bool _running = true;

        /// <summary>Something happened. Client will have to take care of thread issues.</summary>
        public event EventHandler<ReceiveEventArgs>? Receive;

        /// <summary>The canceller.</summary>
        readonly ManualResetEvent _cancelEvent = new(false);

        /// <summary>My logger.</summary>
        readonly MpLog? _log = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Identifier.</param>
        /// <param name="logfn">Optional.</param>
        public Server(string pipeName, string? logfn)
        {
            _pipeName = pipeName;
            if (logfn is not null)
            {
                _log = new(logfn, "SERVER");
            }
        }

        /// <summary>
        /// Run it.
        /// </summary>
        public void Start()
        {
            _thread = new(ServerThread);
            _thread.Start();
        }

        /// <summary>
        /// Stop the server - called from main.
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            bool ok = true;

            _log?.Write($"Stop()");

            _running = false;
            _cancelEvent.Set();

            _log?.Write($"Shutting down");
            _thread?.Join();
            _log?.Write($"Thread ended");
            _thread = null;

            return ok;
        }

        /// <summary>
        /// Required.
        /// </summary>
        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }

            _cancelEvent.Dispose();
        }

        /// <summary>
        /// Listen for client messages. Interruptible by setting _cancelEvent.
        /// </summary>
        void ServerThread()
        {
            _log?.Write($"Main thread started");

            try
            {
                while (_running)
                {
                    using NamedPipeServerStream stream = new(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    using AutoResetEvent connectEvent = new(false);

                    ReceiveEventArgs evt = new();

                    _log?.Write($"BeginWaitForConnection()");

                    AsyncCallback callBack = new(ProcessClient);

                    ConnectionState cst = new();

                    stream.BeginWaitForConnection(callBack, cst);

                    // Check for events of interest.
                    int sig = WaitHandle.WaitAny(new WaitHandle[] { connectEvent, _cancelEvent });
                    switch (sig)
                    {
                        case 0:
                            // Normal, ignore.
                            break;

                        case 1:
                            _log?.Write($"Normal stop signal");
                            _running = false;
                            break;

                        default:
                            _log?.Write($"Unknown wait result:{sig}");
                            _running = false;
                            break;
                    }

                    ///// The actual worker callback.
                    void ProcessClient(IAsyncResult ar)
                    {
                        // This is running in a new thread. Wait for something to show up.
                        if (ar is not null && ar.AsyncState is not null)
                        {
                            ConnectionState state = (ConnectionState)(ar.AsyncState);

                            try
                            {
                                _log?.Write($"EndWaitForConnection()");
                                stream.EndWaitForConnection(ar);
                                _log?.Write($"Client wants to tell us something");

                                state.Status = ConnectionStatus.Receiving;

                                while (state.Status == ConnectionStatus.Receiving)
                                {
                                    // The total number of bytes read into the buffer or 0 if the end of the stream has been reached.
                                    var numRead = stream.Read(state.Buffer, state.BufferIndex, state.Buffer.Length - state.BufferIndex);
                                    _log?.Write($"num read:{numRead}");

                                    if (numRead > 0)
                                    {
                                        state.BufferIndex += numRead;

                                        // Full string arrived?
                                        int terminator = Array.IndexOf(state.Buffer, (byte)'\n');
                                        if (terminator >= 0)
                                        {
                                            // Make buffer into a string.
                                            string msg = new UTF8Encoding().GetString(state.Buffer, 0, terminator);

                                            _log?.Write($"Got message:{msg}");

                                            // Process the line.
                                            evt.Message = msg;
                                            evt.Error = false;

                                            // Reset.
                                            state.BufferIndex = 0;
                                            state.Status = ConnectionStatus.ValidMessage;
                                        }
                                    }

                                    // Wait a bit.
                                    Thread.Sleep(50);
                                }
                            }
                            catch (ObjectDisposedException er)
                            {
                                state.Status = ConnectionStatus.Error;
                                evt.Message = $"Client pipe is closed: {er.Message}";
                                evt.Error = true;
                            }
                            catch (IOException er)
                            {
                                state.Status = ConnectionStatus.Error;
                                evt.Message = $"Client pipe connection has been broken: {er.Message}";
                                evt.Error = true;
                            }
                            catch (Exception er)
                            {
                                state.Status = ConnectionStatus.Error;
                                evt.Message = $"Client pipe unknown exception: {er.Message}";
                                evt.Error = true;
                            }

                            // Hand back what we captured.
                            Receive?.Invoke(this, evt);
                        }

                        // Signal completion.
                        connectEvent.Set();
                    }
                    ///// End of ProcessClient() callback
                }
            }
            catch (Exception ee)
            {
                // General server error.
                Receive?.Invoke(this, new ReceiveEventArgs()
                {
                    Message = $"Unknown server exception: {ee.Message}",
                    Error = true
                });
            }
    
            _log?.Write($"Main thread ended");
        }
    }
    #endregion

    #region Client

    /// <summary>Possible outcomes.</summary>
    public enum ClientStatus { Ok, Timeout, Error }

    /// <summary>Companion client to server. This runs in a new process.</summary>
    public class Client
    {
        /// <summary>Pipe name.</summary>
        readonly string _pipeName;

        /// <summary>My logger.</summary>
        readonly MpLog? _log = null;

        /// <summary>Caller may be able to use this.</summary>
        public string Error { get; set; } = "";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Pipe name to use.</param>
        /// <param name="logfn">Optional log.</param>
        public Client(string pipeName, string? logfn = null)
        {
            _pipeName = pipeName;
            if (logfn is not null)
            {
                _log = new(logfn, "CLIENT");
            }
        }

        /// <summary>
        /// Blocking send string.
        /// </summary>
        /// <param name="s">String to send.</param>
        /// <param name="timeout">Msec to wait for completion.</param>
        /// <returns></returns>
        public ClientStatus Send(string s, int timeout)
        {
            ClientStatus res = ClientStatus.Ok;

            try
            {
                using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                _log?.Write($"rcv:{s}");
                pipeClient.Connect(timeout);
                byte[] outBuffer = new UTF8Encoding().GetBytes(s + "\n");
                pipeClient.Write(outBuffer, 0, outBuffer.Length);
                pipeClient.WaitForPipeDrain();
                // Now exit.
            }
            catch (TimeoutException)
            {
                // Client can deal with this.
                _log?.Write($"timed out", true);
                res = ClientStatus.Timeout;
            }
            catch (Exception ex)
            {
                // Other error.
                _log?.Write($"{ex}", true);
                Error = ex.ToString();
                res = ClientStatus.Error;
            }

            return res;
        }
    }
    #endregion

    #region Common
    /// <summary>
    /// A simple logger which handles client calls from multiple processes/threads.
    /// This is not intended to be a general purpose logger but one that serves a specific purpose,
    /// to debug the SimpleIpc component.
    /// </summary>
    public class MpLog
    {
        /// <summary>File lock id.</summary>
        const string MUTEX_GUID = "65A7B2CE-D1A1-410F-AA57-1146E9B29E02";

        /// <summary>Which file.</summary>
        readonly string _filename;

        /// <summary>For sorting.</summary>
        readonly string _category = "????";

        /// <summary>Rollover size.</summary>
        readonly int _maxSize = 10000;

        /// <summary>
        /// Init the log file.
        /// </summary>
        /// <param name="filename">The file.</param>
        /// <param name="category">The category.</param>
        public MpLog(string filename, string category)
        {
            _filename = filename;
            int catSize = 6;
            _category = category.Length >= catSize ? category.Left(catSize) : category.PadRight(catSize);

            // Good time to check file size.
            using var mutex = new Mutex(false, MUTEX_GUID);

            mutex.WaitOne();
            FileInfo fi = new(_filename);
            if (fi.Exists && fi.Length > _maxSize)
            {
                string ext = fi.Extension;
                File.Copy(fi.FullName, fi.FullName.Replace(ext, "_old" + ext), true);
                Clear();
            }
            mutex.ReleaseMutex();
        }

        /// <summary>
        /// Add a line.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="error">T/F</param>
        public void Write(string s, bool error = false)
        {
            var se = error ? "!!! ERROR !!!" : "";
            s = $"{DateTime.Now:mm\\:ss\\.fff} {_category} {Environment.ProcessId, 5} {Thread.CurrentThread.ManagedThreadId, 2} {se} {s}{Environment.NewLine}";

            using var mutex = new Mutex(false, MUTEX_GUID);

            mutex.WaitOne();
            File.AppendAllText(_filename, s);
            mutex.ReleaseMutex();
        }

        /// <summary>
        /// Empty the log file.
        /// </summary>
        public void Clear()
        {
            File.WriteAllText(_filename, "");
        }
        #endregion
    }
}
