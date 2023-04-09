using System;
using System.Linq;
using System.Text;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// Transforms a <see href="StompFrame" /> into a byte stream (network).
    /// </summary>
    internal class StompMarshaller : IMarshaller
    {
        private readonly ILogger<StompDriver> _logger;

        public StompMarshaller(ILogger<StompDriver> logger)
        {
            _logger = logger;
        }

        public byte[] Marshal(StompFrame frame)
        {
            frame.Validate();

            var body = frame.GetBodyBytes();
#if DEBUG
            var bodyBinary = BinaryPrinter.GetHex(body);
            _logger.LogTrace(
                StompEventIds.Marshaller,
                $"Outgoing message\r\n{frame.ToString(withBody: false)}\r\nBinary frame part (body):\r\n", bodyBinary);
#endif
            var sb = new StringBuilder();
            sb.AppendLine(frame.Command);
            sb.AppendLine($"{StompConstants.Headers.ContentLength}:{body.Length}");

            var headerNames = frame.GetHeaderNames();
            var sortedHeaderKeys = new string[headerNames.Length];
            Array.Copy(headerNames, sortedHeaderKeys, headerNames.Length);
            Array.Sort(sortedHeaderKeys);
            foreach (var key in sortedHeaderKeys)
            {
                var values = frame.GetHeaderValues(key);
                foreach (var value in values)
                {
                    sb.AppendLine($"{StompUtilities.EncodeHeader(key)}:{StompUtilities.EncodeHeader(value)}");
                }
            }

            sb.AppendLine();
            var commandWithHeaders = Encoding.UTF8.GetBytes(sb.ToString());
#if DEBUG
            var headerBinary = BinaryPrinter.GetHex(commandWithHeaders);
            _logger.LogDebug(
                StompEventIds.Marshaller,
                $"Outgoing message\r\n{frame.ToString(withBody: false)}\r\nBinary frame part (header):\r\n", headerBinary);
#endif
            return commandWithHeaders.Concat(body).Concat(new[] { ByteConstants.Null }).ToArray();
        }
    }
}
