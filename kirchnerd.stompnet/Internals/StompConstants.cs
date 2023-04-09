#pragma warning disable CS0414
namespace kirchnerd.StompNet.Internals
{
    internal class StompConstants
    {
        /// <summary>
        /// Common stomp commands.
        /// </summary>
        public static class Commands
        {
            public static string Undefined => nameof(Undefined).ToUpper();

            public static string Heartbeat => nameof(Heartbeat).ToUpper();

            public static string Connect => nameof(Connect).ToUpper();

            public static string Stomp => nameof(Stomp).ToUpper();

            public static string Send => nameof(Send).ToUpper();

            public static string Subscribe => nameof(Subscribe).ToUpper();

            public static string Unsubscribe => nameof(Unsubscribe).ToUpper();

            public static string Begin => nameof(Begin).ToUpper();

            public static string Commit => nameof(Commit).ToUpper();

            public static string Abort => nameof(Abort).ToUpper();

            public static string Ack => nameof(Ack).ToUpper();

            public static string Nack => nameof(Nack).ToUpper();

            public static string Disconnect => nameof(Disconnect).ToUpper();

            public static string Message => nameof(Message).ToUpper();

            public static string Error => nameof(Error).ToUpper();

            public static string Receipt => nameof(Receipt).ToUpper();

            public static string Connected => nameof(Connected).ToUpper();
        }

        /// <summary>
        /// Common stomp headers.
        /// </summary>
        public static class Headers
        {
            public static readonly string Version = "version";

            public static readonly string Host = "host";

            public static readonly string Login = "login";

            public static readonly string Passcode = "passcode";

            public static readonly string HeartBeat = "heart-beat";

            public static readonly string Session = "session";

            public static string Server = "server";

            public static readonly string AcceptVersion = "accept-version";

            public static string MessageId = "message-id";

            public static readonly string Subscription = "id";

            public static readonly string SubscriptionId = "subscription";

            public static readonly string Ack = "ack";

            public static readonly string AckId = "id";

            public static readonly string ReplyTo = "reply-to";

            public static readonly string ContentLength = "content-length";

            public static readonly string ContentType = "content-type";

            public static readonly string Destination = "destination";

            public static readonly string Receipt = "receipt";

            public static readonly string ReceiptId = "receipt-id";

            public static readonly string Transaction = "transaction";

            public static class Internal
            {
                public static readonly string Received = "$stompnet.received";

                public static readonly string Sent = "$stompnet.sent";
            }

        }
    }
}