using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace kirchnerd.StompNet
{
    /// <summary>
    /// Protocol-related settings of the driver component
    /// </summary>
    public class StompOptions
    {
        /// <summary>
        /// The host (stomp server) to which the underlying TCP connection is made.
        /// </summary>
        public string Host { get; set; } = null!;

        /// <summary>
        /// The port number to which the underlying TCP connection is made.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The name of a configured instance of the message broker. If the
        /// broker is able to handle multiple tenants than each tenant connects to
        /// a different vhost.
        /// </summary>
        public string VirtualHost { get; set; } = "/";


        /// <summary>
        /// The frequency in milliseconds at which the client wants to receive a heartbeat from the stomp server.
        /// </summary>
        public int IncomingHeartBeat { get; set; }

        /// <summary>
        /// The frequency in milliseconds at which the client can send (guaranteed) heartbeats to the stomp server.
        /// </summary>
        public int OutgoingHeartBeat { get; set; }

        /// <summary>
        /// The login username of the client.
        /// </summary>
        public string? Login { get; set; }

        /// <summary>
        /// the login password of the client.
        /// </summary>
        public string? Passcode { get; set; }

        /// <summary>
        /// Configures the tcp keepalive-timeout in milliseconds for sent messages.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// Configures the tcp keepalive-timeout in milliseconds for received messages.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// A logger component for the driver.
        /// </summary>
        public ILogger<StompDriver> Logger { get; set; } = new NullLogger<StompDriver>();

        /// <summary>
        /// Flag whether TLS should be enabled or not.
        /// </summary>
        public bool Tls { get; set; }

        /// <summary>
        /// Updates the the stomp options with the given name value collection.
        /// </summary>
        /// <param name="nvc">The name value collection to use for update.</param>
        internal void Update(NameValueCollection nvc)
        {
            if (nvc.AllKeys.Contains(nameof(IncomingHeartBeat)))
            {
                var incomingHeartBeat = nvc.Get(nameof(IncomingHeartBeat));
                if (int.TryParse(incomingHeartBeat, out var cx))
                {
                    IncomingHeartBeat = cx;
                }
            }

            if (nvc.AllKeys.Contains(nameof(OutgoingHeartBeat)))
            {
                var outgoingHeartBeat = nvc.Get(nameof(OutgoingHeartBeat));
                if (!int.TryParse(outgoingHeartBeat, out var cy))
                {
                    OutgoingHeartBeat = cy;
                }
            }

            if (nvc.AllKeys.Contains(nameof(SendTimeout)))
            {
                var sendTimeout = nvc.Get(nameof(SendTimeout));
                if (int.TryParse(sendTimeout, out var sendTime))
                {
                    SendTimeout = sendTime;
                }
            }

            if (!nvc.AllKeys.Contains(nameof(ReceiveTimeout))) return;
            var receiveTimeout = nvc.Get(nameof(ReceiveTimeout));
            if (int.TryParse(receiveTimeout, out var receiveTime))
            {
                ReceiveTimeout = receiveTime;
            }
        }
    }
}