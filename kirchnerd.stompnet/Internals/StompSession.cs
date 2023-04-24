using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using kirchnerd.StompNet.Validators;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Implementation of a STOMP Session. Have a look at <see href="ISession" /> for further details.
    /// </summary>
    internal sealed class StompSession : ISession
    {
        private readonly object _sync = new();

        private readonly ILogger<StompDriver> _logger;
        private readonly StompClient _stompClient;
        private IServerSpecificValidator _frameValidator = null!;
        private IReplyHeaderProvider _replyHeaderProvider = null!;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler? Closed;
        public void OnClosed(object sender, EventArgs e)
        {
            Closed?.Invoke(this, e);
        }

        public StompSession(
            ILogger<StompDriver> logger,
            IConnection connection,
            StompClient stompClient,
            string server,
            string sessionId)
        {
            Id = sessionId;
            Connection = connection;
            Server = server;
            InitServerSpecificHandlers();
            _stompClient = stompClient;
            _logger = logger;
        }

        private void InitServerSpecificHandlers()
        {
            ServerBaseStrategy strategy;
            switch (Server)
            {
                case var value when value.StartsWith("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    strategy = new RabbitMqStrategy();
                    break;
                default:
                    strategy = new AnyServerStrategy();
                    break;
            }

            _frameValidator = strategy;
            _replyHeaderProvider = strategy;
        }

        public SessionState State { get; private set; } = SessionState.Established;

        /// <inheritdoc />
        public IConnection Connection { get; }

        public string Server { get; }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public bool IsClosed => State == SessionState.Closed;

        /// <inheritdoc />
        public IDestination Get(string destination)
        {
            return new StompDestination(this, _stompClient, _frameValidator, _replyHeaderProvider, destination);
        }

        /// <inheritdoc />
        public Task Ack(string id)
        {
            var ackFrame = StompFrame.CreateAck(id);
            return _stompClient.SendAsync(ackFrame);
        }

        /// <inheritdoc />
        public Task Nack(string id)
        {
            var nackFrame = StompFrame.CreateNack(id);
            return _stompClient.SendAsync(nackFrame);
        }

        /// <inheritdoc />
        public async Task<MessageFrame> RequestAsync(string destination, SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: true));
            var response = await _stompClient.RequestAsync(frame, _replyHeaderProvider.GetReplyHeader, timeout);
            return (MessageFrame)response;
        }

        /// <inheritdoc />
        public void Send(string destination, SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: false));
            _stompClient.Send(frame, timeout);
        }

        /// <inheritdoc />
        public Task SendAsync(string destination, SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: false));
            return _stompClient.SendAsync(frame, timeout);
        }

        /// <inheritdoc />
        public Task<bool> SubscribeAsync(string id, string destination,
            RequestHandlerAsync handler, AcknowledgeMode acknowledgeMode)
        {
            return _stompClient.SubscribeAsync(
                id,
                destination,
                this,
                async (msg) => await handler((MessageFrame) msg, this),
                acknowledgeMode);
        }

        /// <inheritdoc />
        public Task<bool> SubscribeAsync(string id, string destination,
            SendHandlerAsync handler, AcknowledgeMode acknowledgeMode)
        {
            return _stompClient.SubscribeAsync(
                id,
                destination,
                this,
                async msg =>
                {
                    await handler((MessageFrame)msg, this);
                    return SendFrame.Void();
                },
                acknowledgeMode);
        }

        /// <inheritdoc />
        public Task<bool> UnsubscribeAsync(string id)
        {
            return _stompClient.UnsubscribeAsync(id);
        }

        /// <summary>
        /// Closes the communication over stomp and free up resources on server and client side.
        /// </summary>
        public void Close()
        {
            if (State == SessionState.Closed)
            {
                return;
            }

            CloseInternal(disconnect: true);
        }

        internal void CloseInternal(bool disconnect)
        {
            _logger.LogInformation(
                StompEventIds.CloseSession,
                $"Shutdown STOMP-Session: '{Id}' due to client disconnect.");
            lock (_sync)
            {
                if (State != SessionState.Established)
                {
                    return;
                }

                if (disconnect)
                {
                    try
                    {
                        // send disconnect frame to stomp server
                        _logger.LogTrace(
                            StompEventIds.Disconnect,
                            "Disconnect from STOMP-Server");
                        Disconnect();
                        _stompClient.Stop();
                        _logger.LogInformation(
                            StompEventIds.Disconnect,
                            "Disconnected from STOMP-Server");
                    }
                    catch (Exception)
                    {
                        _logger.LogError(
                            StompEventIds.Disconnect,
                            "Error during disconnect...");
                    }
                }

                State = SessionState.Closed;
            }

            _logger.LogInformation(
                StompEventIds.CloseSession,
                $"Shutdown STOMP-Session: Session with '{Id}' was terminated successfully.");
        }

        private void Disconnect()
        {
            var disconnectFrame = StompFrame.CreateDisconnect();
            _stompClient.Send(disconnectFrame);
        }

        public string Dump()
        {
            return $"{Connection.Dump()}, Session.Id={Id}, Session.State={Enum.GetName(typeof(SessionState), State)}";
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _stompClient.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
