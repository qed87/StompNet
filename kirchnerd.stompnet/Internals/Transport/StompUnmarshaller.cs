using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// This class implements the basic logic to parse an incoming byte stream into a stomp wire frame.
    /// It's implemented as a recursive descent parser (LL-grammar).
    /// </summary>
    /// <remarks>https://stomp.github.io/stomp-specification-1.2.html#Augmented_BNF</remarks>
    internal class StompUnmarshaller : IUnmarshaller
    {
        private readonly ILogger<StompDriver> _logger;

        public StompUnmarshaller(ILogger<StompDriver> logger)
        {
            _logger = logger;
        }

        public StompFrame Unmarshal(FrameBytesRead readByte, CancellationToken cancelToken)
        {
            if (readByte == null)
            {
                throw new ArgumentNullException(nameof(readByte));
            }

            var context = new UnmarshalContext(readByte, cancelToken);

            var octet = context.ReadByte();
            switch (octet)
            {
                case ByteConstants.Null:
                    return StompFrame.CreateShutdown();
                case ByteConstants.CarriageReturn:
                {
                    octet = context.ReadByte();
                    if (octet == ByteConstants.LineFeed)
                    {
                        return StompFrame.CreateHeartbeat(FrameType.Server);
                    }

                    context.ReturnBytes(2);
                    break;
                }
                case ByteConstants.LineFeed:
                    return StompFrame.CreateHeartbeat(FrameType.Server);
                default:
                    context.ReturnByte();
                    break;
            }

            var frame = ReadFrame(context);
            return frame;
        }

        private static int? GetLength(NameValueCollection nvc)
        {
            var contentLengths = nvc.GetValues("content-length");
            if (contentLengths == null || contentLengths.Length == 0)
            {
                return null;
            }

            var contentLength = contentLengths.First();
            if (!int.TryParse(contentLength, out var length))
            {
                return null;
            }

            return length;
        }

        private static byte[] ReadBody(UnmarshalContext ctx, int? length = null)
        {
            byte[] bodyOctets;
            if (length.HasValue)
            {
                bodyOctets = new byte[length.Value];
                ctx.ReadBytes(bodyOctets, 0, length.Value);
                ctx.ReadByte(); // the frame still must be terminated with 0x00 octet
            }
            else
            {
                var octets = new List<byte>();
                byte octet;
                while ((octet = ctx.ReadByte()) != ByteConstants.Null)
                {
                    octets.Add(octet);
                }

                bodyOctets = octets.ToArray();
            }

            return bodyOctets;
        }

        private static NameValueCollection ReadHeaders(UnmarshalContext ctx)
        {
            var nvc = new NameValueCollection();
            while (!ctx.IsEndOfLine())
            {
                var headerBytes = ctx.ReadLine();
                var headerLine = Encoding.UTF8.GetString(headerBytes);
                var headerKeyValueTuple = headerLine.Split(':');
                var key = StompUtilities.DecodeHeader(headerKeyValueTuple[0]);
                string? value = null;
                if (headerKeyValueTuple.Length > 1)
                {
                    value = StompUtilities.DecodeHeader(headerKeyValueTuple[1]);
                }

                nvc.Add(key, value);
            }

            ctx.ReadEndOfLine(); // header and body are separated with mandatory EOL
            return nvc;
        }

        private static string ReadCommand(UnmarshalContext ctx)
        {
            var octets = ctx.ReadLine();
            var command = Encoding.UTF8.GetString(octets);
            return command;
        }

        private StompFrame ReadFrame(UnmarshalContext ctx)
        {
            var command = ReadCommand(ctx);

            var nvp = ReadHeaders(ctx);

            var length = GetLength(nvp);

            var body = ReadBody(ctx, length);

#if DEBUG
            var bodyBinary = BinaryPrinter.GetHex(body);
            var header = string.Join(
                "\r\n",
                nvp.AllKeys.Where(key => key != null).OrderBy(key => key)
                    .SelectMany(key => nvp.GetValues(key)!.Select((value) => $"{key}:{value}")));
            _logger.LogDebug(
                StompEventIds.Unmarshaller,
                $"Incoming Message\r\n{command}\r\n{header}\r\nBinary frame part (body):\r\n", bodyBinary);
#endif

            return StompFrame.Create(command, nvp, body);
        }

    }
}
