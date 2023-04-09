using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Represents a destination to which clients can SUBSCRIBE, UNSUBSCRIBE or SEND frames to.
    /// </summary>
    internal class StompDestination : IDestination
    {
        private readonly ISession _session;
        private readonly StompClient _stompClient;
        private readonly string _destination;

        public StompDestination(
            ISession session,
            StompClient stompClient,
            string destination)
        {
            _session = session;
            _stompClient = stompClient;
            _destination = destination;
        }

        /// <inheritdoc />
        public async Task<MessageFrame> RequestAsync(SendFrame frame, int timeout)
        {
            frame.WithDestination(_destination);
            var response = await _stompClient.RequestAsync(frame, timeout);
            return (MessageFrame)response;
        }

        /// <inheritdoc />
        public void Send(SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(_destination);
            _stompClient.Send(frame, timeout);
        }

        /// <inheritdoc />
        public Task SendAsync(SendFrame frame, int timeout = 1000)
        {
            frame.WithDestination(_destination);
            return _stompClient.SendAsync(frame, timeout);
        }

        /// <inheritdoc />
        public Task<bool> SubscribeAsync(string id, FrameHandlerAsync handler, AcknowledgeMode acknowledgeMode)
        {
            return _stompClient.SubscribeAsync(id, _destination, _session,
                handler, acknowledgeMode);
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
