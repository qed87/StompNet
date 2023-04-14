using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Interfaces
{
    /// <summary>
    /// This interface represents a STOMP session. Each connection to a Message Broker via the stomp protocol may only contain one session.
    ///
    /// Only when a STOMP session is started, the communication between client and broker via the Stomp protocol is started.
    ///
    /// The session stores the settings negotiated with the server(e.g.heartbeat and protocol version) and manages all subscriptions to destinations.
    /// </summary>
    public interface ISession : IDisposable
    {
        event EventHandler Closed;

        /// <summary>
        /// Session Id of this session.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The state of the current session.
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// Provides a destination with the given name <paramref name="destination" />.
        /// </summary>
        /// <param name="destination">The destination to address.</param>
        /// <returns>A destination on the stomp server.</returns>
        IDestination Get(string destination);

        /// <summary>
        /// Signals the server that the message was consumed. This is a positive acknowledgment.
        /// </summary>
        /// <param name="id">The message ack-id.</param>
        public Task Ack(string id);

        /// <summary>
        /// Signals the server that the message was not consumed. This is a negative acknowledgment
        /// and the server may deliver the message to another client.
        /// </summary>
        /// <param name="id">The message ack-id.</param>
        public Task Nack(string id);

        /// <summary>
        /// Asynchronously sends a request over stomp.
        /// </summary>
        /// <param name="destination">The destination to send the message.</param>
        /// <param name="frame">The send frame.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The response frame.</returns>
        Task<MessageFrame> RequestAsync(
            string destination,
            SendFrame frame,
            int timeout = 1000);

        /// <summary>
        /// Synchronously sends a message over stomp.
        /// </summary>
        /// <param name="destination">The destination to send the message.</param>
        /// <param name="frame">The send frame.</param>
        /// <param name="timeout">The timeout.</param>
        void Send(
            string destination,
            SendFrame frame,
            int timeout = 1000);

        /// <summary>
        /// Asynchronously sends a message over stomp.
        /// </summary>
        /// <param name="destination">The destination to send the message.</param>
        /// <param name="frame">The send frame.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>A task which is completed when the was successfully send.</returns>
        Task SendAsync(
            string destination,
            SendFrame frame,
            int timeout = 1000);

        /// <summary>
        /// Creates a subscription which starts a message flow from destination (broker-side)
        /// to the client.
        /// </summary>
        Task<bool> SubscribeAsync(
            string id,
            string destination,
            RequestHandlerAsync handler,
            AcknowledgeMode acknowledgeMode);

        /// <summary>
        /// Creates a subscription which starts a message flow from destination (broker-side)
        /// to the client.
        /// </summary>
        Task<bool> SubscribeAsync(
            string id,
            string destination,
            SendHandlerAsync handler,
            AcknowledgeMode acknowledgeMode);

        /// <summary>
        /// Removes a existing subscription from the session. This stops the message
        /// flow from the broker to the client for the destination of the subscription.
        /// </summary>
        Task<bool> UnsubscribeAsync(string id);

        /// <summary>
        /// The connection this session belongs to.
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Flag which is true when the session is closed and false otherwise.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Close the session.
        /// </summary>
        void Close();

        /// <summary>
        /// Prints detailed (technical) information about this session.
        /// </summary>
        /// <returns></returns>
        string Dump();

    }
}
