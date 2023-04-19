using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Extensions;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Middleware;
using kirchnerd.StompNet.Internals.Services;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Implements the STOMP protocol behavior and represents a middleware indirectly used by clients.
    /// </summary>
    /// <remarks>
    /// Orchestrates the messaging components.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal sealed class StompClient : IDisposable
    {
        public event ErrorHandler? Error;
        internal void OnError(Exception ex)
        {
            Error?.Invoke(ex);
        }

        private bool _disposed;
        private readonly string _connection;
        private readonly FrameBytesRead _readBytes;
        private readonly FrameBytesWrite _writeBytes;

        private readonly ILogger<StompDriver> _logger;
        private readonly IReceiptService _receiptService;
        private readonly IAcknowledgeService _acknowledgeService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHeartbeatService _heartbeatIn;
        private readonly IHeartbeatService _heartbeatOut;
        private readonly CancellationTokenSource _cancelSource = new();

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
            _receiptService = new ReceiptService();
            _heartbeatIn = new InboxHeartbeatService(this);
            _heartbeatOut = new OutboxHeartbeatService(this);
            _subscriptionService = new SubscriptionService(_logger);
            _acknowledgeService = new AcknowledgeService(logger, _subscriptionService);

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

        public ConnectedFrame Connect(ConnectFrame connectFrame)
        {
            using var listenerDisposal = _subscriptionService.AddReplyListener(
                frame => string.Equals(frame.Command, StompConstants.Commands.Connected,
                    StringComparison.OrdinalIgnoreCase));
            Send(connectFrame);
            return (ConnectedFrame) listenerDisposal.GetResult().GetAwaiter().GetResult();
        }

        public async Task<bool> SubscribeAsync(
            string id,
            string queue,
            ISession session,
            RequestHandlerInternalAsync clientCallback,
            AcknowledgeMode acknowledgeMode = AcknowledgeMode.Auto)
        {
            if (!_subscriptionService.AddListener(id, session, GetHandler(clientCallback), acknowledgeMode))
                return false;
            _logger.LogInformation(
                StompEventIds.StompClient,
                $"Subscribe to '{queue}' with name='{id}' on connection '{_connection}'.");
            var subscribeFrame = StompFrame.CreateSubscribe(id, queue, acknowledgeMode);
            subscribeFrame.WithReceipt();
            await SendAsync(subscribeFrame, CancellationToken.None);
            return true;

        }

        private Func<StompFrame, string, AcknowledgeMode, Task> GetHandler(RequestHandlerInternalAsync clientCallback)
        {
            return async (frame, subscriptionId, acknowledgeMode) =>
            {
                var messageFrame = (MessageFrame)frame;
                _logger.LogTrace(
                    StompEventIds.StompClient,
                    $"Received frame {frame.ToString()} on subscription='{subscriptionId}', connection='{_connection}'.");

                // call client handler
                var response = (SendFrame)await clientCallback(messageFrame);

                if (response != SendFrame.Void() && frame.HasHeader(StompConstants.Headers.ReplyTo))
                {
                    var replyTo = frame.GetHeader(StompConstants.Headers.ReplyTo);
                    _logger.LogTrace(
                        StompEventIds.StompClient,
                        $"Sending reply to '{replyTo}' for frame {frame.ToString()} on subscription='{subscriptionId}', connection='{_connection}'.");
                    response.WithDestination(replyTo);
                    await SendAsync(response, CancellationToken.None);
                }

                if (acknowledgeMode == AcknowledgeMode.Auto)
                {
                    return;
                }

                var acknowledged = messageFrame.IsAcknowledged();

                // the client want to send ack/nack on his own.
                // This makes in most cases sense for client-mode since we want to acknowledge
                // a batch of messages. The implemented client-mode could lead to acknowledged
                // messages which shouldn't be acknowledged. So be careful.
                if (!acknowledged.HasValue)
                {
                    return;
                }

                await SendAcknowledge(frame, subscriptionId, acknowledged.Value);
            };
        }

        /// <summary>
        /// Send ack/nack. This frame may be filtered out by the outbox middleware.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="acknowledged">
        /// True when the message should be acknowledged.
        /// False when the frame should be not acknowledged.
        /// </param>
        private async Task SendAcknowledge(StompFrame frame, string subscriptionId, bool acknowledged)
        {
            if (!frame.HasHeader(StompConstants.Headers.Ack)) return;

            _logger.LogTrace(
                StompEventIds.StompClient,
                $"Send acknowledge for {frame.ToString()} on subscription='{subscriptionId}', connection='{_connection}'.");
            var ackId = frame.GetHeader(StompConstants.Headers.Ack);
            StompFrame ackNack;
            if (acknowledged)
            {
                ackNack = StompFrame.CreateAck(ackId);
            }
            else
            {
                ackNack = StompFrame.CreateNack(ackId);
            }

            await SendAsync(ackNack, CancellationToken.None);
        }

        public async Task<bool> UnsubscribeAsync(string id)
        {
            _logger.LogInformation(
                StompEventIds.StompClient,
                $"Unsubscribe from '{id}' on connection '{_connection}'.");
            var frame = StompFrame.CreateUnsubscribe(id);
            frame.WithReceipt();
            await SendAsync(frame);
            return _subscriptionService.RemoveListener(id);
        }

        public void Send(StompFrame frame, int timeout = 1000)
        {
            SendAsync(frame, timeout).GetAwaiter().GetResult();
        }

        public void Send(StompFrame frame, CancellationToken cancellationToken)
        {
            SendAsync(frame, cancellationToken).GetAwaiter().GetResult();
        }

        public Task SendAsync(StompFrame frame, int timeout = 1000)
        {
            CancellationTokenSource cts = new();
            cts.CancelAfter(timeout);

            return SendAsync(frame, cts.Token);
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

            using var listenerDisposal = _subscriptionService.AddReplyListener(
                receivedFrame =>
                    string.Equals(
                        receivedFrame.Command,
                        StompConstants.Commands.Message,
                        StringComparison.OrdinalIgnoreCase) &&
                    receivedFrame.HasHeader(StompConstants.Headers.SubscriptionId) &&
                    string.Equals(
                        receivedFrame.GetHeader(StompConstants.Headers.SubscriptionId),
                        sendFrame.GetHeader(StompConstants.Headers.ReplyTo)));
            await SendAsync(sendFrame, cancellationToken);
            return await listenerDisposal.GetResult();
        }

        private async Task SendAsync(StompFrame frame, CancellationToken cancellationToken)
        {
            var receiptTask = Task.CompletedTask;
            if (frame.HasHeader(StompConstants.Headers.Receipt))
            {
                receiptTask = _receiptService.WaitForReceiptAsync(frame.GetHeader(StompConstants.Headers.Receipt));
            }

            await _outbox.EnqueueAsync(frame, cancellationToken);
            await Task.WhenAny(
                cancellationToken.AsTask(),
                receiptTask);

            if (frame.HasHeader(StompConstants.Headers.Receipt))
            {
                _receiptService.TryRemove(frame.GetHeader(StompConstants.Headers.Receipt));
            }

            if (!receiptTask.IsCompleted)
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// This method creates a new outbox agent (running in his own thread) for the current connection.
        /// This agent is responsible for receiving and sending frames to a message broker.
        /// </summary>
        private void StartUpStompOutbox()
        {
            var pipeline = new Pipeline<OutboxContext>();
            // log frames and errors
            pipeline.Use(async (ctx, next) =>
            {
                try
                {
                    _logger.LogDebug(
                        StompEventIds.Outbox,
                        $"Received frame {ctx.Frame.ToString()}.");
                    await next();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        StompEventIds.Outbox,
                        ex.ToStringDemystified());
                }
            });

            // This middleware is invoked before a frame is transmitted over the wire and
            // sorts out ack/nack's which are redundant.
            pipeline.Use(async (ctx, next) =>
            {
                if (ctx.Frame is not IAcknowledge acknowledge)
                {
                    await next();
                    return;
                }

                if (!_acknowledgeService.IsAcknowledgeRequired(acknowledge.Id, out var subscriptionId, out var timestamp))
                    return;

                await next();

                if (subscriptionId is null || timestamp is null) return;
                _acknowledgeService.Update(subscriptionId, timestamp.Value);
            });

            // update outgoing heartbeat
            pipeline.Use(async (ctx, next) =>
            {
                await next();
                // After Sent
                _logger.LogTrace(
                    StompEventIds.Outbox,
                    $"Update outgoing heartbeat.");
                _heartbeatOut.Update();
            });

            pipeline.Run(ctx =>
            {
                _writeBytes!(ctx.FrameBytes);
                return Task.CompletedTask;
            });

            _outbox = new StompOutbox(
                _connection,
                _logger,
                new MarshallerProvider(),
                pipeline,
                _cancelSource.Token);
            _outbox.Error += OnError;
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
            var pipeline = new Pipeline<InboxContext>();
            // log frames and errors
            pipeline.Use(async (ctx, next) =>
            {
                try
                {
                    _logger.LogDebug(
                        StompEventIds.Inbox,
                        $"Received frame {ctx.Frame.ToString()}.");
                    await next();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        StompEventIds.Inbox,
                        ex.ToStringDemystified());
                }
            });
            // update heartbeat
            pipeline.Use((ctx, next) =>
            {
                _logger.LogTrace(
                    StompEventIds.StompClient,
                    $"Update incoming heartbeat.");
                _heartbeatIn.Update();
                return next();
            });
            // handle incoming frames
            pipeline.Run(ctx =>
            {
                var frame = ctx.Frame;
                switch (frame.Command)
                {
                    case var value when string.Equals(value, StompConstants.Commands.Receipt, StringComparison.OrdinalIgnoreCase):
                        var receiptId = frame.GetHeader(StompConstants.Headers.ReceiptId);
                        _receiptService.Receive(receiptId);
                        break;
                    case var value when string.Equals(value, StompConstants.Commands.Connected, StringComparison.OrdinalIgnoreCase):
                        _subscriptionService.NotifyListeners(frame);
                        break;
                    case var value when string.Equals(value, StompConstants.Commands.Message, StringComparison.OrdinalIgnoreCase):
                        var subscriptionId = frame.GetHeader(StompConstants.Headers.SubscriptionId);
                        if (frame.HasHeader(StompConstants.Headers.Ack))
                        {
                            _logger.LogDebug(
                                StompEventIds.Inbox,
                                $"Create acknowledgment for {frame.ToString()} for subscription '{subscriptionId}'.");
                            var ackId = frame.GetHeader(StompConstants.Headers.Ack);
                            // track acknowledge id from the server (broker) that must be handled.
                            _acknowledgeService.Register(ackId, subscriptionId, frame.GetHeader<long>(StompConstants.Headers.Internal.Received));
                        }

                        _subscriptionService.NotifyListeners(frame);
                        break;
                    case var value when string.Equals(value, StompConstants.Commands.Error, StringComparison.OrdinalIgnoreCase):
                        OnError(new StompFrameException(frame, frame.GetBody()));
                        break;
                    default:
                        _logger.LogWarning(
                            StompEventIds.Inbox,
                            $"Unknown frame: {frame.ToString()}");
                        break;
                }

                return Task.CompletedTask;
            });

            _inbox = new StompInbox(
                _logger,
                _connection,
                new StompUnmarshaller(_logger),
                pipeline,
                _readBytes,
                _cancelSource.Token);
            _inbox.Error += OnError;
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
                _acknowledgeService.Dispose();
                _subscriptionService.Dispose();
                _receiptService.Dispose();

                if (_inboxThread.IsAlive)
                {
                    _inbox.Error -= OnError;
                    _inbox.Stop();
                    _cancelSource.Cancel();
                    _inboxThread.Join();
                }

                if (_outboxThread.IsAlive)
                {
                    _outbox.Error -= Error;
                    _outbox.Stop();
                    _cancelSource.Cancel();
                    _outboxThread.Join();
                }
            }

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
    }
}
