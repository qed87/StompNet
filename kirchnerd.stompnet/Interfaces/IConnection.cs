using System;

namespace kirchnerd.StompNet.Interfaces
{
    /// <summary>
    /// Interface of a STOMP connection
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// The current state of the connection.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// Uniquely identifies this connection.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets a value that indicates whether the connection is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Close the current connection and sessions which belong to this connection.
        /// </summary>
        void Close();

        /// <summary>
        /// Prints detailed (technical) information about this connection.
        /// </summary>
        /// <returns></returns>
        string Dump();
    }
}
