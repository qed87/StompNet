using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using kirchnerd.StompNet.Validators;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Represents a destination to which clients can SUBSCRIBE, UNSUBSCRIBE or SEND frames to.
    /// </summary>
    internal class StompDestination : IDestination
    {
        private readonly ISession _session;
        private readonly StompClient _stompClient;
        private readonly IServerSpecificValidator _frameValidator;
        private readonly IReplyHeaderProvider _replyHeaderProvider;
        private readonly string _destination;

        public StompDestination(
            ISession session,
            StompClient stompClient,
            IServerSpecificValidator frameValidator,
            IReplyHeaderProvider replyHeaderProvider,
            string destination)
        {
            _session = session;
            _stompClient = stompClient;
            _frameValidator = frameValidator;
            _replyHeaderProvider = replyHeaderProvider;
            _destination = destination;
        }

        /// <inheritdoc />
        public async Task<MessageFrame> RequestAsync(SendFrame frame, int timeout)
        {
            frame.WithDestination(_destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: true));
            var response = await _stompClient.RequestAsync(frame, _replyHeaderProvider.GetReplyHeader, timeout);
            return (MessageFrame)response;
        }

        /// <inheritdoc />
        public void Send(SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(_destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: false));
            _stompClient.Send(frame, timeout);
        }

        /// <inheritdoc />
        public Task SendAsync(SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(_destination);
            _frameValidator.Validate(new ValidationContext(frame, isRequest: false));
            return _stompClient.SendAsync(frame, timeout);
        }

        /// <inheritdoc />
        public Task<bool> SubscribeAsync(string id, RequestHandlerAsync handler, AcknowledgeMode acknowledgeMode)
        {
            return _stompClient.SubscribeAsync(
                id,
                _destination,
                _session,
                async msg => await handler((MessageFrame) msg, _session),
                acknowledgeMode);
        }

        /// <inheritdoc />
        public Task<bool> SubscribeAsync(string id, SendHandlerAsync handler, AcknowledgeMode acknowledgeMode)
        {
            return _stompClient.SubscribeAsync(
                id,
                _destination,
                _session,
                async msg =>
                {
                    await handler((MessageFrame) msg, _session);
                    return SendFrame.Void();
                },
                acknowledgeMode);
        }

        /// <inheritdoc />
        public Task<bool> UnsubscribeAsync(string id)
        {
            return _stompClient.UnsubscribeAsync(id);
        }

        /// <inheritdoc />
        public Task AckAsync(string id)
        {
            var ackFrame = StompFrame.CreateAck(id);
            return _stompClient.SendAsync(ackFrame);
        }

        /// <inheritdoc />
        public Task NackAsync(string id)
        {
            var nackFrame = StompFrame.CreateNack(id);
            return _stompClient.SendAsync(nackFrame);
        }
    }
}
