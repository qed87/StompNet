using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Extensions;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    /// <summary>
    /// A Stomp frame which is used to transport information from client to broker and in reverse.
    /// </summary>
    public abstract class StompFrame
    {
        private byte[] _bodyOctets = Array.Empty<byte>();
        private readonly NameValueCollection _headers = new(StringComparer.OrdinalIgnoreCase);

        protected StompFrame(string command, FrameType frameType, int priority)
        {
            Command = command;
            Type = frameType;
            Priority = priority;
        }

        /// <summary>
        /// Gets the Command of the frame.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// The type of the frame. If the frame is designated for the STOMP-Server than this property is 'Server'.
        /// Otherwise this property is 'Client'.
        /// </summary>
        /// <remarks>
        /// See STOMP documentation for an overview of client/server frames.
        /// </remarks>
        public FrameType Type { get; }

        /// <summary>
        /// The
        /// </summary>
        internal int Priority { get; }

        /// <summary>
        /// The headers of the frame.
        /// </summary>
        public IReadOnlyDictionary<string, string?> Headers => GetHeaderNames()
            .ToDictionary(key => key, key => _headers[key]);

        /// <summary>
        /// Sets the body and the contentType of the frame.
        /// </summary>
        public void SetBody(string body, string contentType)
        {
            SetBody(body);
            SetHeader(StompConstants.Headers.ContentType, contentType);
        }

        /// <summary>
        /// Sets the body of the frame.
        /// </summary>
        internal void SetBody(string body)
        {
            EnsureBodyIsValid();
            var bytes = Encoding.UTF8.GetBytes(body);
            SetBody(bytes);
        }

        /// <summary>
        /// Sets the body of the frame.
        /// </summary>
        internal void SetBody(byte[] octets)
        {
            DeleteHeader(StompConstants.Headers.ContentType);
            _bodyOctets = octets;
        }

        protected virtual void EnsureBodyIsValid()
        {
            if (HasHeader(StompConstants.Headers.ContentLength))
            {
                throw new StompException($"Header '{StompConstants.Headers.ContentLength}' must not be set explicitly.");
            }
        }

        /// <summary>
        /// Gets the body of the frame.
        /// </summary>
        public string GetBody()
        {
            return Encoding.UTF8.GetString(GetBodyBytes());
        }

        public static SendFrame CreateSend()
        {
            return new SendFrame();
        }

        public static MessageFrame CreateMessage()
        {
            return new MessageFrame();
        }

        public static ConnectFrame CreateConnect(string acceptedVersion, string host, string heartbeat)
        {
            var connectFrame = new ConnectFrame();
            connectFrame.SetHeader(StompConstants.Headers.AcceptVersion, acceptedVersion);
            connectFrame.SetHeader(StompConstants.Headers.Host, host);
            connectFrame.SetHeader(StompConstants.Headers.HeartBeat, heartbeat);
            return connectFrame;
        }

        public static ConnectedFrame CreateConnected()
        {
            return new ConnectedFrame();
        }

        internal static SubscribeFrame CreateSubscribe(string id, string queue, AcknowledgeMode mode)
        {
            var subscribeFrame = new SubscribeFrame();
            subscribeFrame.SetHeader(StompConstants.Headers.Subscription, id);
            subscribeFrame.SetHeader(StompConstants.Headers.Destination, queue);
            subscribeFrame.SetHeader(StompConstants.Headers.Ack, mode.ToHeaderValue());
            subscribeFrame.WithReceipt();
            return subscribeFrame;
        }

        internal static UnsubscribeFrame CreateUnsubscribe(string id)
        {
            var unsubscribeFrame = new UnsubscribeFrame();
            unsubscribeFrame.SetHeader(StompConstants.Headers.Subscription, id);
            unsubscribeFrame.WithReceipt();
            return unsubscribeFrame;
        }

        internal static AckFrame CreateAck(string id)
        {
            var ack = new AckFrame();
            ack.SetHeader(StompConstants.Headers.AckId, id);
            return ack;
        }

        internal static NackFrame CreateNack(string id)
        {
            var nack = new NackFrame();
            nack.SetHeader(StompConstants.Headers.AckId, id);
            return nack;
        }

        internal static DisconnectFrame CreateDisconnect()
        {
            var disconnectFrame = new DisconnectFrame();
            disconnectFrame.WithReceipt();
            return disconnectFrame;
        }

        internal static StompFrame Create(string command, NameValueCollection nvp, byte[] octets)
        {
            StompFrame frame;
            if (string.Equals(command, StompConstants.Commands.Connected, StringComparison.OrdinalIgnoreCase))
            {
                frame = new ConnectedFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Message, StringComparison.OrdinalIgnoreCase))
            {
                frame = new MessageFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Error, StringComparison.OrdinalIgnoreCase))
            {
                frame = new ErrorFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Receipt, StringComparison.OrdinalIgnoreCase))
            {
                frame = new ReceiptFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Connect, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, StompConstants.Commands.Stomp, StringComparison.OrdinalIgnoreCase))
            {
                frame = new ConnectFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Subscribe, StringComparison.OrdinalIgnoreCase))
            {
                frame = new SubscribeFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Unsubscribe, StringComparison.OrdinalIgnoreCase))
            {
                frame = new UnsubscribeFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Ack, StringComparison.OrdinalIgnoreCase))
            {
                frame = new AckFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Nack, StringComparison.OrdinalIgnoreCase))
            {
                frame = new NackFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Abort, StringComparison.OrdinalIgnoreCase))
            {
                frame = new AbortFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Commit, StringComparison.OrdinalIgnoreCase))
            {
                frame = new CommitFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Send, StringComparison.OrdinalIgnoreCase))
            {
                frame = new SendFrame();
            }
            else if (string.Equals(command, StompConstants.Commands.Disconnect, StringComparison.OrdinalIgnoreCase))
            {
                frame = new DisconnectFrame();
            }
            else
            {
                throw new StompException($"Unknown frame '{command}'.");
            }

            foreach (var eachKey in nvp.AllKeys)
            {
                if (eachKey is null) continue;
                frame.SetHeader(eachKey, nvp.Get(eachKey));
            }

            frame.SetBody(octets);
            frame.Validate();
            return frame;
        }

        internal static StompFrame CreateShutdown()
        {
            return new SocketShutdownFrame();
        }

        internal static StompFrame CreateHeartbeat(FrameType frameType)
        {
            return new HeartbeatFrame(frameType);
        }

        /// <summary>
        /// Validates the frame.
        /// </summary>
        public virtual void Validate() { }

        public void SetHeader(string key, string? value)
        {
            EnsureHeaderIsValid(key, value);
            var values = _headers.GetValues(key);
            var newValues = new[] { value }.Concat(values ?? Array.Empty<string>()).ToArray();
            _headers.Set(key, string.Join(",", newValues));
        }

        internal void DeleteHeader(string key)
        {
            _headers.Remove(key);
        }

        protected static void EnsureHeaderIsValid(string key, string? value)
        {
            if (string.Equals(key, StompConstants.Headers.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                throw new StompValidationException($"Reserved Header '{StompConstants.Headers.Transaction}' is not supported.");
            }
        }

        public string[] GetHeaderNames()
            => _headers.AllKeys.Where(key => key != null).ToArray()!;

        public T GetHeader<T>(string key)
        {
            var value = GetHeader(key);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public string GetHeader(string key)
        {
            return _headers.GetValues(key)!.First();
        }

        public IReadOnlyList<string> GetHeaderValues(string key)
        {
            return _headers.GetValues(key)?.ToArray() ?? Array.Empty<string>();
        }

        public bool HasHeader(string key)
        {
            return _headers.AllKeys.Contains(key);
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool withBody = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Command);

            var sortedHeaderKeys = new string[_headers.AllKeys.Length];
            Array.Copy(_headers.AllKeys, sortedHeaderKeys, _headers.AllKeys.Length);
            Array.Sort(sortedHeaderKeys);
            foreach (var key in sortedHeaderKeys)
            {
                var values = _headers.GetValues(key);
                if (values == null)
                {
                    sb.AppendLine($"{key}:<null>");
                    continue;
                }

                foreach (var value in values)
                {
                    sb.AppendLine($"{key}:{value ?? "<null>" }");
                }
            }
            sb.AppendLine();
            if (withBody)
            {
                sb.AppendLine(GetBody());
            }

            return sb.ToString();
        }

        internal byte[] GetBodyBytes()
        {
            return _bodyOctets;
        }

        internal void ResetBody()
        {
            SetBody(Array.Empty<byte>());
        }

        internal void MarkReceived()
        {
            DeleteHeader(StompConstants.Headers.Internal.Received);
            SetHeader(
                StompConstants.Headers.Internal.Received,
                Convert.ToString(DateTimeOffset.Now.Ticks));
        }

        internal void MarkSend()
        {
            DeleteHeader(StompConstants.Headers.Internal.Sent);
            SetHeader(
                StompConstants.Headers.Internal.Sent,
                Convert.ToString(DateTimeOffset.Now.Ticks));
        }
    }
}
