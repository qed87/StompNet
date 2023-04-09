using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet
{
    /// <summary>
    /// Collection of constants that are used as identifiers for tracing
    /// </summary>
    internal static class StompEventIds
    {
        public static readonly EventId ConnectOverTcp = new(0, nameof(ConnectOverTcp));

        public static readonly EventId StompHandshake = new(1, nameof(StompHandshake));

        public static readonly EventId ConnectionClosed = new(2, nameof(ConnectionClosed));

        public static readonly EventId ConnectionShutdown = new(3, nameof(ConnectionShutdown));

        public static readonly EventId SocketRead = new(4, nameof(SocketRead));

        public static readonly EventId SocketWrite = new(5, nameof(SocketWrite));

        public static readonly EventId CloseSession = new(6, nameof(CloseSession));

        public static readonly EventId Disconnect = new(7, nameof(Disconnect));

        public static readonly EventId WorkerThreads = new(8, nameof(WorkerThreads));

        public static readonly EventId Inbox = new(9, nameof(Inbox));

        public static readonly EventId Outbox = new(10, nameof(Outbox));

        public static readonly EventId StompClient = new(11, nameof(StompClient));

        public static readonly EventId Marshaller = new(12, nameof(Marshaller));

        public static readonly EventId Unmarshaller = new(13, nameof(Unmarshaller));

    }
}
