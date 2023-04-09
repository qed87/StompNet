using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet
{
    /// <summary>
    /// Implements a STOMP connection. Look at <see href="IConnection" /> for further details.
    /// </summary>
    internal sealed class StompConnection : IConnection
    {
        private readonly StompOptions _stompOptions;
        private readonly ILogger<StompDriver> _logger;
        private Stream _network;
        private TcpClient _tcpClient;

        private readonly object _sync = new();

        private StompClient _stompClient = null!;
        private StompSession _session = null!;
        private volatile ConnectionState _state = ConnectionState.Pending;

        /// <summary>
        /// A stomp connection is responsible for managing connection-related resources such as threads and sockets.
        /// Each stomp connection is associated with exactly one session. And each stomp session belongs to exactly one connection.
        ///
        /// Only when a stomp session is created the communication on the tcp-socket via the stomp protocol is started.
        /// The session is responsible for the negotiation of the connection behaviour and manages all session related logins and resources.
        /// </summary>
        internal StompConnection(TcpClient tcpClient, Stream networkStream, StompOptions options)
        {
            _stompOptions = options;
            _network = networkStream;
            _tcpClient = tcpClient;
            _logger = options.Logger;
        }

        public string Id => $"{_tcpClient!.Client.LocalEndPoint}->{_tcpClient.Client.RemoteEndPoint}";

        public bool IsClosed => _state == ConnectionState.Closed;

        public ConnectionState State => _state;

        /// <summary>
        /// Creates a new stomp session if none exists yet.
        /// The creation of a stomp session involves following steps:
        ///  1) Send a connect frame to the server
        ///  2) wait for a connected frame
        ///  3) determine the negotiated protocol parameters
        ///  4) Initialize driver components
        /// </summary>
        internal ISession? CreateSession()
        {
            lock (_sync)
            {
                switch (_state)
                {
                    case ConnectionState.Closed:
                        throw new StompException("Connection is closed and cannot be restarted");
                    case ConnectionState.Established:
                        return _session;
                    case ConnectionState.Pending:
                        try
                        {
                            _logger.LogInformation(
                                StompEventIds.StompHandshake,
                                $"Establishing STOMP-Session on '{Id}'");
                            // this value determines if and how often the client can send outgoing heartbeats
                            var cx = _stompOptions.OutgoingHeartBeat;
                            // this value determines if and how often the client want to receive incoming heartbeats from the server
                            var cy = _stompOptions.IncomingHeartBeat;

                            _stompClient = new StompClient(Id, ReadByte, WriteBytes, _logger);

                            // Send connect frame
                            var connectFrame = StompFrame.CreateConnect("1.2", _stompOptions.VirtualHost, $"{cx},{cy}");
                            if (_stompOptions.Login is not null)
                            {
                                connectFrame.SetHeader(StompConstants.Headers.Login, _stompOptions.Login);
                            }

                            if (_stompOptions.Passcode is not null)
                            {
                                connectFrame.SetHeader(StompConstants.Headers.Passcode, _stompOptions.Passcode);
                            }

                            var listenerId = _stompClient.AddListener(
                                frame => string.Equals(frame.Command, StompConstants.Commands.Connected,
                                    StringComparison.OrdinalIgnoreCase));
                            _stompClient.Send(connectFrame);
                            var connectedFrame = _stompClient.ListenAndRemove(listenerId);

                            if (connectedFrame is null) throw new StompException("Connection failed.");
                            ConfigureHeartbeat(connectedFrame, cx, cy);

                            _session = new StompSession(
                                _logger,
                                this,
                                _stompClient,
                                connectedFrame.GetHeader("session"));

                            _state = ConnectionState.Established;

                            _logger.LogInformation(StompEventIds.StompHandshake, $"Established STOMP-Session on '{Id}'");

                            return _session;
                        }
                        catch (Exception ex)
                        {
                            _state = ConnectionState.Closed;
                            _logger.LogError(StompEventIds.StompHandshake, ex, $"Error while establishing STOMP session on '{Id}'.");
                            Close();
                            throw;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void ConfigureHeartbeat(StompFrame connected, int cx, int cy)
        {
            var heartBeat = connected.GetHeader("heart-beat");
            var heartBeatParts = heartBeat.Split(',');

            // this value determines if and how often the server can send outgoing heartbeats
            var sx = int.Parse(heartBeatParts[0]);
            // this value determines if and how often the server want to receive incoming heartbeats
            var sy = int.Parse(heartBeatParts[1]);

            var (x, y) = NegotiatedHeartBeat(cx, cy, sx, sy);
            // send heart-beat
            if (x > 0)
            {
                _stompClient!.SetHeartbeatOut(x - (long)Math.Floor(x * 0.5));
            }

            // receive heart-beat
            if (y > 0)
            {
                _stompClient!.SetHeartbeatIn(y + 500); /* give the server a little bit more time if he stands under heavy load */
            }

            _logger.LogInformation(StompEventIds.StompHandshake, $"Negotiated Heartbeat on '{Id}' is ({x}, {y})");
        }

        /// <summary>
        /// Closes the tcp socket and gives up associated resources.
        /// </summary>
        public void Close()
        {
            _logger.LogWarning(StompEventIds.ConnectionClosed,
                $"STOMP Connection was terminated by the client.");
            CloseInternal(disconnect: true);
        }

        private void CloseInternal(bool disconnect)
        {
            if (_state == ConnectionState.Closed)
            {
                return;
            }

            lock (_sync)
            {
                if (_state != ConnectionState.Established)
                {
                    return;
                }

                _logger.LogCritical(StompEventIds.ConnectionShutdown, $"Begin shutdown connection with '{Id}'.");
                try
                {
                    // Disconnect from server...
                    CloseSession(disconnect);

                    _network.Close();

                    _tcpClient.Close();

                    _state = ConnectionState.Closed;

                    _session.OnClosed(_session, EventArgs.Empty);

                }
                catch (Exception ex)
                {
                    _logger.LogError(StompEventIds.ConnectionShutdown, $"Error during shutdown tcp-connection: {ex.Message}");
                }
                finally
                {
                    _logger.LogError(StompEventIds.ConnectionClosed, $"Finished shutdown connection with '{Id}'.");

                    Dispose(true);

                }
            }
        }

        public string Dump()
        {
            return $"Connection.Id={Id}, Connection.State={Enum.GetName(typeof(ConnectionState), _state)}";
        }

        /// <summary>
        /// Read next byte from network stream.
        /// </summary>
        private byte ReadByte(CancellationToken cancelToken)
        {
            try
            {
                var buffer = new byte[1];
                // ReSharper disable once MustUseReturnValue
                _network.Read(buffer, 0, 1);
                return buffer[0];
            }
            catch (Exception exception)
            {
                if (_state == ConnectionState.Established)
                {
                    _logger.LogCritical(
                        StompEventIds.SocketRead,
                        $"Error while reading from net socket: {exception.Message}.");
                }

                throw;
            }
        }

        /// <summary>
        /// Write bytes to the network stream.
        /// </summary>
        private void WriteBytes(byte[] message)
        {
            try
            {
                _network.Write(message, 0, message.Length);
            }
            catch (Exception exception)
            {
                if (_state == ConnectionState.Established)
                {
                    _logger.LogCritical(
                        StompEventIds.SocketWrite,
                        $"Error while writing to net socket: {exception.Message}");
                }

                throw;
            }
        }

        private static (int, int) NegotiatedHeartBeat(int cx, int cy, int sx, int sy)
        {
            int x, y;
            if (cx > 0 && sy > 0)
            {
                x = Math.Max(cx, sy);
            }
            else
            {
                // either the server cannot receive heart-beats or the client cannot send heart-beats
                x = 0;
            }

            if (cy > 0 && sx > 0)
            {
                y = Math.Max(cy, sx);
            }
            else
            {
                // either the client doesn't want to receive heart-beats or the server cannot send heart-beats
                y = 0;
            }

            return (x, y);
        }

        private void CloseSession(bool disconnect)
        {
            _session.CloseInternal(disconnect);
        }

        #region IDisposable Support

        private bool _disposed;
        ~StompConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose managed state (managed objects)
                _session.Dispose();
                _network.Dispose();
                _tcpClient.Dispose();
                _stompClient.Dispose();

                Debug.WriteLine("Disposing connection...");

            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            _disposed = true;
        }

        #endregion IDisposable Support
    }
}
