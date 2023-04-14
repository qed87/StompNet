using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Interfaces
{
    /// <summary>
    /// Abstraction of a well-know destination on the broker site, i.e. a queue or an exchange.
    ///
    /// A STOMP server is modeled as a set of destinations to which messages can be sent.
    /// The STOMP protocol treats destinations as opaque string and their syntax is server
    /// implementation specific. Additionally STOMP does not define what the delivery semantics
    /// of destinations should be. The delivery, or “message exchange”, semantics of destinations
    /// can vary from server to server and even from destination to destination.
    ///
    /// A STOMP client is a user-agent which can act in two (possibly simultaneous) modes:
    ///  - as a producer, sending messages to a destination on the server via a SEND frame
    ///  - as a consumer, sending a SUBSCRIBE frame for a given destination and receiving messages from the server as MESSAGE frames.
    /// </summary>
    public interface IDestination
    {
        /// <summary>
        /// Asynchronously sends a request over stomp.
        /// </summary>
        /// <returns>The response frame.</returns>
        Task<MessageFrame> RequestAsync(
            SendFrame frame,
            int timeout);

        /// <summary>
        /// Synchronously sends a message over stomp.
        /// </summary>
        void Send(
            SendFrame frame,
            int timeout = 1000);

        /// <summary>
        /// Asynchronously sends a message over stomp.
        /// </summary>
        /// <returns>A task which is completed when the was successfully send.</returns>
        Task SendAsync(
            SendFrame frame,
            int timeout = 1000);

        /// <summary>
        /// Signals the server that the message was consumed. This is a positive acknowledgment.
        /// </summary>
        /// <param name="id">The message ack-id.</param>
        public Task AckAsync(string id);

        /// <summary>
        /// Signals the server that the message was not consumed. This is a negative acknowledgment
        /// and the server may deliver the message to another client.
        /// </summary>
        /// <param name="id">The message ack-id.</param>
        public Task NackAsync(string id);

        /// <summary>
        /// Creates a subscription which starts a message flow from destination (broker-side)
        /// to the client.
        /// </summary>
        Task<bool> SubscribeAsync(
            string id,
            RequestHandlerAsync handler,
            AcknowledgeMode acknowledgeMode);

        /// <summary>
        /// Creates a subscription which starts a message flow from destination (broker-side)
        /// to the client.
        /// </summary>
        Task<bool> SubscribeAsync(
            string id,
            SendHandlerAsync handler,
            AcknowledgeMode acknowledgeMode);

        /// <summary>
        /// Removes a existing subscription from the session. This stops the message flow from
        /// the broker to the client for the destination of the subscription.
        /// </summary>
        Task<bool> UnsubscribeAsync(string id);

    }
}