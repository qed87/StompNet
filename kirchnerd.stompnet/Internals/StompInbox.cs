using System;
using System.Threading;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// This class represents the inbox of the driver component and listens
    /// on the tcp socket connection to read new data packets from the broker and pass them into the system.
    /// </summary>
    /// <remarks>
    /// Only one thread may read the socket connection at a time but a simultaneously write is allowed.
    /// </remarks>
    internal class StompInbox : IDisposable
    {
        public event ErrorHandler? Error;
        protected void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }

        public event FrameHandlerInternal? FrameReceived;
        protected void OnFrameReceived(StompFrame frame)
        {
            FrameReceived?.Invoke(frame);
        }

        private readonly CancellationToken _cancelToken;

        private readonly IUnmarshaller _unmarshaller;

        private readonly ILogger<StompDriver> _logger;

        private readonly string _connectionString;

        private volatile bool _isRunning = true;

        private FrameBytesRead? _readByte;

        public StompInbox(ILogger<StompDriver> logger,
            string connectionString,
            IUnmarshaller unmarshaller,
            FrameBytesRead readByte,
            CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            _connectionString = connectionString;
            _logger = logger;
            _readByte = readByte;
            _unmarshaller = unmarshaller;
        }

        /// <summary>
        /// This method represents the entry point for the thread which is responsible for reading messages from the broker.
        /// </summary>
        public void Run()
        {
            try
            {
                while (_isRunning)
                {
                    // read next frame
                    var frame = _unmarshaller.Unmarshal(_readByte!, _cancelToken);
                    frame.MarkReceived();
                    _logger.LogDebug(
                        StompEventIds.Inbox,
                        $"Frame read on '{_connectionString}':\r\n\r\n{frame}");

                    if (_cancelToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        _logger.LogDebug(
                            StompEventIds.Inbox,
                            $"Forward Frame to StompClient on '{_connectionString}'::\r\n\r\n{frame}");

                        OnFrameReceived(frame);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            StompEventIds.Inbox,
                            $"Error on '{_connectionString}':\r\n\r\n{ex}\r\n\r\nduring forward frame:\r\n\r\n{frame}");
                        OnError(new StompFrameException(frame, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    StompEventIds.Inbox,
                    $"Error during read on '{_connectionString}':':\r\n\r\n{ex}");
                Stop();
                // inbox agent can only be closed on the hard way since thread interrupts are ignored...
                OnError(ex);
            }
        }

        public void Stop()
        {
            _logger.LogInformation(
                StompEventIds.Inbox,
                $"Stop Inbox requested on '{_connectionString}'.");
            _isRunning = false;
        }

        #region IDisposable Support

        private bool _dispose = false;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_dispose) return;
            if (disposing)
            {
                // dispose managed resources here
            }

            // release unmanaged resources here
            _readByte = null;
            _dispose = true;
        }
        #endregion IDisposable Support
    }
}
