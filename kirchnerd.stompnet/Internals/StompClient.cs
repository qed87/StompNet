using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Extensions;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Implements the STOMP protocol behavior and represents a middleware indirectly used by clients.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal sealed class StompClient : IDisposable
    {
        public event ErrorHandler? Error;
        private void OnError(Exception ex)
        {
            Error?.Invoke(ex);
        }

        private long _lastAck;

        private bool _disposed;
        private readonly string _connection;
        private readonly FrameBytesRead _readBytes;
        private readonly FrameBytesWrite _writeBytes;

        private readonly ILogger<StompDriver> _logger;
        private readonly ReceiptTimer _receiptTimer;
        private readonly HeartbeatTimer _heartbeatIn;
        private readonly HeartbeatTimer _heartbeatOut;
        private readonly CancellationTokenSource _cancelSource = new();
        private readonly ConcurrentDictionary<string, Listener> _listeners = new();
        private readonly ConcurrentDictionary<string, Acknowledgement> _acknowledged = new();

        private StompOutbox _outbox = null!;
        private Thread _outboxThread = null!;
        private StompInbox _inbox = null!;
        private Thread _inboxThread = null!;

        public StompClient(
            string connection,
            FrameBytesRead readBytes,
            FrameBytesWrite writeBytes,
            ILogger<StompDriver> logger)
        {
            _connection = connection;
            _readBytes = readBytes;
            _writeBytes = writeBytes;
            _logger = logger;
            _receiptTimer = new ReceiptTimer();
            _heartbeatIn = new HeartbeatTimer(() => OnError(new StompHeartbeatMissingException()));
            _heartbeatOut = new HeartbeatTimer(
                () => Send(StompFrame.CreateHeartbeat(FrameType.Client),
                CancellationToken.None));

            StartUpStompInbox();
            StartUpStompOutbox();
        }

        /// <summary>
        /// Stops the stomp client.
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation(
                StompEventIds.StompClient,
                $"Stop stomp client requested.");
            _cancelSource.Cancel();
        }

        private void OnFrameReceived(StompFrame frame)
        {
            Received(frame);
        }

        /// <summary>
        /// This callback is invoked by the stomp inbox whenever a new frame is received.
        /// This method is invoked within the inbox thread, therefor no blocking should happen here.
        /// </summary>
        /// <param name="frame">The received frame.</param>
        private void Received(StompFrame frame)
        {
            _logger.LogDebug(
                StompEventIds.StompClient,
                $"Received frame {frame.ToString()}.");
            var shouldNotify = false;
            if (string.Equals(frame.Command, StompConstants.Commands.Receipt,
                StringComparison.OrdinalIgnoreCase))
            {
                var receiptId = frame.GetHeader(StompConstants.Headers.ReceiptId);
                _receiptTimer.Receive(receiptId);
            }
            else if (string.Equals(frame.Command, StompConstants.Commands.Message,
                StringComparison.OrdinalIgnoreCase))
            {
                var subscriptionId = frame.GetHeader(StompConstants.Headers.SubscriptionId);
                if (frame.HasHeader(StompConstants.Headers.Ack))
                {
                    // The server requires an acknowledgment of the given message.
                    var ackId = frame.GetHeader(StompConstants.Headers.Ack);
                    _logger.LogDebug(
                        StompEventIds.StompClient,
                        $"Create acknowledgment for {frame.ToString()} for subscription '{subscriptionId}'.");
                    var acknowledgment = new Acknowledgement(
                        subscriptionId,
                        frame.GetHeader<long>(StompConstants.Headers.Internal.Received));
                    _acknowledged.TryAdd(
                        ackId,
                        acknowledgment);
                }

                shouldNotify = true;
            }
            else if (string.Equals(frame.Command, StompConstants.Commands.Connected, StringComparison.OrdinalIgnoreCase))
            {
                shouldNotify = true;
            }
            else if (string.Equals(frame.Command, StompConstants.Commands.Error, StringComparison.OrdinalIgnoreCase))
            {
                OnError(new StompFrameException(frame, frame.GetBody()));
            }
            else
            {
                _logger.LogWarning(
                    StompEventIds.Inbox,
                    $"Unknown frame: {frame.ToString()}");
            }

            UpdateIncomingHeartbeat();

            if (shouldNotify)
                NotifyListeners(frame);
        }

        private void UpdateIncomingHeartbeat()
        {
            _logger.LogTrace(
                StompEventIds.StompClient,
                $"Update incoming heartbeat.");
            _heartbeatIn.Update();
        }

        /// <summary>
        /// Callback invoked before a frame is transmitted over the wire.
        /// This callback filters out ack/nack's which are redundant.
        /// </summary>
        /// <param name="sendingContext">The sending context.</param>
        public void Sending(SendingContext sendingContext)
        {
            var frame = sendingContext.Frame;
            if (!string.Equals(frame.Command, StompConstants.Commands.Ack, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(frame.Command, StompConstants.Commands.Nack, StringComparison.OrdinalIgnoreCase)) return;
            if (!_acknowledged.TryRemove(frame.GetHeader(StompConstants.Headers.AckId), out var acknowledgment) ||
                !_listeners.TryGetValue(acknowledgment.SubscriptionId, out var listener)) return;
            var subscription = (SubscriptionState)listener.State;
            switch (subscription.AcknowledgeMode)
            {
                case AcknowledgeMode.Auto:
                    _logger.LogDebug(
                        StompEventIds.StompClient,
                        $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' is in mode 'auto'.");
                    sendingContext.Cancel();
                    return;
                case AcknowledgeMode.Client when acknowledgment.Timestamp <= _lastAck:
                    _logger.LogDebug(
                        StompEventIds.StompClient,
                        $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' "
                        + "is in mode 'client' and a more recent message frame is already acknowledged.");
                    sendingContext.Cancel();
                    return;
                default:
                    _logger.LogTrace(
                        StompEventIds.StompClient,
                        $"Update acknowledge timestamp.");
                    Interlocked.Exchange(ref _lastAck, acknowledgment.Timestamp);
                    break;
            }
        }

        /// <summary>
        /// Callback which is invoked after a new frame is sent. This handler updates the outgoing heartbeat.
        /// </summary>
        /// <param name="ctx"></param>
        public void Sent(SentContext ctx)
        {
            _logger.LogTrace(
                StompEventIds.StompClient,
                $"Update outgoing heartbeat.");
            _heartbeatOut.Update();
        }

        /// <summary>
        /// Detects custom handlers which are interested at the given frame
        /// and starts a worker to invoke them.
        /// </summary>
        /// <param name="frame"></param>
        private async void NotifyListeners(StompFrame frame)
        {
            var handlerTasks = new List<Task>();
            foreach (var (_, listener) in _listeners.ToArray())
            {
                if (!listener.Check(frame))
                {
                    continue;
                }

                handlerTasks.Add(Task.Run(() => listener.Handler.Invoke(frame, listener.State)));
            }

            await Task.WhenAll(handlerTasks);
        }

        internal StompFrame ListenAndRemove(string id)
        {
            return ListenAndRemoveAsync(id).GetAwaiter().GetResult();
        }

        private async Task<StompFrame> ListenAndRemoveAsync(string id)
        {
            if (!_listeners.TryGetValue(id, out var listener))
                throw new ApplicationException($"Listener '{id}' is already removed.");

            try
            {
                var state = (ListenerState)listener.State;
                await state.TaskCompletionSource.Task;
                return state.Result;
            }
            finally
            {
                RemoveListener(id);
            }
        }

        internal string AddListener(Predicate<StompFrame> predicate)
        {
            var listenerId = Guid.NewGuid().ToString().Replace("-", "");
            var listenerState = new ListenerState();
            var listener = new Listener(predicate, (frame, state) =>
            {
                var callbackState = (ListenerState)state;
                if (Interlocked.CompareExchange(ref callbackState.Set, 1, 0) != 0) return Task.CompletedTask;
                callbackState.Result = frame;
                callbackState.ManualResetEvent.Set();
                callbackState.TaskCompletionSource.TrySetResult();
                _logger.LogDebug(
                    StompEventIds.StompClient,
                    $"Listener with Id='{listenerId}' received callback.");

                return Task.CompletedTask;
            }, listenerState);
            if (!_listeners.TryAdd(listenerId, listener)) throw new ApplicationException("Error during registration of listener.");
            _logger.LogDebug(
                StompEventIds.StompClient,
                $"Register listener with Id='{listenerId}'.");
            return listenerId;

        }

        internal bool RemoveListener(string id)
        {
            _logger.LogDebug(
                StompEventIds.StompClient,
                $"Remove listener with Id='{id}'.");
            return _listeners.TryRemove(id, out _);
        }

        public async Task<bool> SubscribeAsync(
            string id,
            string queue,
            ISession session,
            FrameHandlerAsync handler,
            AcknowledgeMode acknowledgeMode = AcknowledgeMode.Auto)
        {
            var listener = new Listener(
                frame => string.Equals(frame.Command, StompConstants.Commands.Message, StringComparison.OrdinalIgnoreCase) &&
                    frame.HasHeader(StompConstants.Headers.SubscriptionId) &&
                    frame.GetHeader(StompConstants.Headers.SubscriptionId) == id,
                HandleMessage,
                new SubscriptionState(id, session, handler, acknowledgeMode));
            if (!_listeners.TryAdd(id, listener)) return false;
            _logger.LogInformation(
                StompEventIds.StompClient,
                $"Subscribe to '{queue}' with name='{id}' on connection '{_connection}'.");
            var subscribeFrame = StompFrame.CreateSubscribe(id, queue, acknowledgeMode);
            subscribeFrame.WithReceipt();
            await SendAsync(subscribeFrame, CancellationToken.None);
            return true;

        }

        private async Task HandleMessage(StompFrame frame, object state)
        {
            var messageFrame = (MessageFrame)frame;
            var subscription = (SubscriptionState)state;

            _logger.LogTrace(
                StompEventIds.StompClient,
                $"Received frame {frame.ToString()} on subscription='{subscription.SubscriptionId}', connection='{_connection}'.");

            // call client handler
            var response = await subscription.Handler.Invoke(messageFrame, subscription.Session);

            if (response != SendFrame.Void() && frame.HasHeader(StompConstants.Headers.ReplyTo))
            {
                var replyTo = frame.GetHeader(StompConstants.Headers.ReplyTo);
                _logger.LogTrace(
                    StompEventIds.StompClient,
                    $"Sending reply to '{replyTo}' for frame {frame.ToString()} on subscription='{subscription.SubscriptionId}', connection='{_connection}'.");
                response.WithDestination(replyTo);
                await SendAsync(response, CancellationToken.None);
            }

            if (subscription.AcknowledgeMode == AcknowledgeMode.Auto)
            {
                return;
            }

            if (frame.HasHeader(StompConstants.Headers.Ack))
            {
                StompFrame ackNack;
                var acknowledged = messageFrame.IsAcknowledged();
                // the client want to send ack/nack on his own.
                // This makes in most cases sense for client-mode since we want to acknowledge
                // a batch of messages. The implemented client-mode could lead to acknowledged
                // messages which shouldn't be acknowledged. So be careful.
                if (!acknowledged.HasValue)
                {
                    return;
                }

                _logger.LogTrace(
                    StompEventIds.StompClient,
                    $"Send acknowledge for {frame.ToString()} on subscription='{subscription.SubscriptionId}', connection='{_connection}'.");
                var ackId = frame.GetHeader(StompConstants.Headers.Ack);
                if (acknowledged.Value)
                {
                    ackNack = StompFrame.CreateAck(ackId);
                }
                else
                {
                    ackNack = StompFrame.CreateNack(ackId);
                }

                await SendAsync(ackNack, CancellationToken.None);
            }
        }

        public async Task<bool> UnsubscribeAsync(string id)
        {
            _logger.LogInformation(
                StompEventIds.StompClient,
                $"Unsubscribe from '{id}' on connection '{_connection}'.");
            var frame = StompFrame.CreateUnsubscribe(id);
            frame.WithReceipt();
            await SendAsync(frame);
            return RemoveListener(id);
        }

        public void Send(StompFrame frame, int timeout = 1000)
        {
            SendAsync(frame, timeout).GetAwaiter().GetResult();
        }

        private void Send(StompFrame frame, CancellationToken cancellationToken)
        {
            SendAsync(frame, cancellationToken).GetAwaiter().GetResult();
        }

        public Task SendAsync(StompFrame frame, int timeout = 1000)
        {
            CancellationTokenSource cts = new();
            cts.CancelAfter(timeout);

            return SendAsync(frame, cts.Token);
        }

        private async Task SendAsync(StompFrame frame, CancellationToken cancellationToken)
        {
            var receiptTask = Task.CompletedTask;
            if (frame.HasHeader(StompConstants.Headers.Receipt))
            {
                receiptTask = _receiptTimer.WaitForReceiptAsync(frame.GetHeader(StompConstants.Headers.Receipt));
            }

            await _outbox.EnqueueAsync(frame, cancellationToken);
            await Task.WhenAny(
                cancellationToken.AsTask(),
                receiptTask);

            if (frame.HasHeader(StompConstants.Headers.Receipt))
            {
                _receiptTimer.TryRemove(frame.GetHeader(StompConstants.Headers.Receipt));
            }

            if (!receiptTask.IsCompleted)
            {
                throw new TimeoutException();
            }
        }

        public StompFrame Request(StompFrame frame, int timespan = 1000)
        {
            CancellationTokenSource cts = new();
            cts.CancelAfter(timespan);
            return RequestAsync(frame, cts.Token).GetAwaiter().GetResult();
        }

        public StompFrame Request(StompFrame frame, CancellationToken cancellationToken)
        {
            return RequestAsync(frame, cancellationToken).GetAwaiter().GetResult();
        }

        public Task<StompFrame> RequestAsync(StompFrame frame, int timespan = 1000)
        {
            CancellationTokenSource cts = new();
            cts.CancelAfter(timespan);
            return RequestAsync(frame, cts.Token);
        }

        public async Task<StompFrame> RequestAsync(
            StompFrame frame,
            CancellationToken cancellationToken)
        {
            if (frame is not SendFrame sendFrame)
            {
                throw new StompException("Request must be SendFrame.");
            }

            if (!sendFrame.HasHeader(StompConstants.Headers.ReplyTo))
            {
                throw new StompException("An explicit reply-to header is mandatory.");
            }

            var listenerId = AddListener(
                receivedFrame => string.Equals(receivedFrame.Command, StompConstants.Commands.Message,
                    StringComparison.OrdinalIgnoreCase) &&
                    receivedFrame.HasHeader(StompConstants.Headers.SubscriptionId) &&
                    string.Equals(
                        receivedFrame.GetHeader(StompConstants.Headers.SubscriptionId),
                        sendFrame.GetHeader(StompConstants.Headers.ReplyTo)));
            if (listenerId is null) throw new ApplicationException("Error while registering a frame listener.");
            await SendAsync(sendFrame, cancellationToken);
            return await ListenAndRemoveAsync(listenerId);
        }

        /// <summary>
        /// This method creates a new outbox agent (running in his own thread) for the current connection.
        /// This agent is responsible for receiving and sending frames to a message broker.
        /// </summary>
        private void StartUpStompOutbox()
        {
            _outbox = new StompOutbox(
                _connection,
                _logger,
                new MarshallerProvider(),
                _writeBytes,
                _cancelSource.Token);
            _outbox.Error += OnError;
            _outbox.Sending += Sending;
            _outbox.Sent += Sent;
            _outboxThread = new Thread(_outbox.Run)
            {
                Name = $"STOMP Outbox Agent @{_connection}",
                IsBackground = false
            };
            _outboxThread.Start();
            _logger.LogInformation(
                StompEventIds.Outbox,
                $"STOMP-Outbox started on '{_connection}'");
        }

        /// <summary>
        /// This method creates a new outbox agent (running in his own thread) for the current connection.
        /// This agent is responsible for reading frames sent by the message broker to the current application.
        /// </summary>
        private void StartUpStompInbox()
        {
            _inbox = new StompInbox(
                _logger,
                _connection,
                new StompUnmarshaller(_logger),
                _readBytes,
                _cancelSource.Token);
            _inbox.Error += OnError;
            _inbox.FrameReceived += OnFrameReceived;
            _inboxThread = new Thread(_inbox.Run)
            {
                Name = $"STOMP Inbox Agent @{_connection}",
                IsBackground = false
            };
            _inboxThread.Start();
            _logger.LogInformation(
                StompEventIds.Outbox,
                $"STOMP-Inbox started on '{_connection}'");
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _heartbeatIn.Dispose();
                _heartbeatOut.Dispose();
                _receiptTimer.Dispose();

                if (_inboxThread.IsAlive)
                {
                    _inbox.Error -= OnError;
                    _inbox.FrameReceived -= OnFrameReceived;
                    _inbox.Stop();
                    _cancelSource.Cancel();
                    _inboxThread.Join();
                }

                if (_outboxThread.IsAlive)
                {
                    _outbox.Sending -= Sending;
                    _outbox.Sent -= Sent;
                    _outbox.Error -= Error;
                    _outbox.Stop();
                    _cancelSource.Cancel();
                    _outboxThread.Join();
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }

        internal void SetHeartbeatOut(long interval)
        {
            _heartbeatOut.Start(interval);
        }

        internal void SetHeartbeatIn(int interval)
        {
            _heartbeatIn.Start(interval);
        }

        private class Acknowledgement
        {
            public Acknowledgement(string subscriptionId, long receivedTicks)
            {
                Timestamp = receivedTicks;
                SubscriptionId = subscriptionId;
            }

            public string SubscriptionId { get; }

            public long Timestamp { get; }
        }

        private class Listener
        {
            public Listener(
                Predicate<StompFrame> predicate,
                Func<StompFrame, object, Task> handler,
                object listenerState)
            {
                Check = predicate ?? throw new ArgumentNullException(nameof(predicate));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
                State = listenerState;
            }

            public Predicate<StompFrame> Check { get; }

            public Func<StompFrame, object, Task> Handler { get; }

            public object State { get; }
        }

        private class SubscriptionState
        {
            public SubscriptionState(
                string subscriptionId,
                ISession session,
                FrameHandlerAsync handler,
                AcknowledgeMode acknowledgeMode = AcknowledgeMode.Auto)
            {
                SubscriptionId = subscriptionId;
                Session = session ?? throw new ArgumentNullException(nameof(session));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
                AcknowledgeMode = acknowledgeMode;
            }

            public string SubscriptionId { get; }

            public ISession Session { get; }

            public FrameHandlerAsync Handler { get; }

            public AcknowledgeMode AcknowledgeMode { get; }

        }

        private class ListenerState
        {
            public int Set;

            public readonly ManualResetEventSlim ManualResetEvent = new();

            public readonly TaskCompletionSource TaskCompletionSource = new();

            public StompFrame Result { get; set; } = null!;
        }
    }
}
