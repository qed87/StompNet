using System.Collections.Generic;
using System.Text;
using kirchnerd.StompNet.Internals.Transport;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Collection of common utility functions which are used by some components within the driver.
    /// </summary>
    internal static class StompUtilities
    {
        /// <summary>
        /// Header keys and values have to be encoded to ensure that no control words are contained within the key or value.
        /// Illegal characters (sequence) which have a special meaning within the message are: Colons, Line-Feed, Carriage Return + Line-Feed and the Backslash.
        /// </summary>
        /// <remarks>https://stomp.github.io/stomp-specification-1.2.html#Value_Encoding</remarks>
        public static string EncodeHeader(string value)
        {
            var encoded = new List<byte>();
            var toEncode = Encoding.UTF8.GetBytes(value);
            foreach (var octet in toEncode)
            {
                switch (octet)
                {
                    case ByteConstants.CarriageReturn:
                        encoded.Add(ByteConstants.Backslash);
                        encoded.Add(ByteConstants.KeyR);
                        break;
                    case ByteConstants.LineFeed:
                        encoded.Add(ByteConstants.Backslash);
                        encoded.Add(ByteConstants.KeyN);
                        break;
                    case ByteConstants.Colon:
                        encoded.Add(ByteConstants.Backslash);
                        encoded.Add(ByteConstants.KeyC);
                        break;
                    case ByteConstants.Backslash:
                        encoded.Add(ByteConstants.Backslash);
                        encoded.Add(ByteConstants.Backslash);
                        break;
                    default:
                        encoded.Add(octet);
                        break;
                }
            }

            return Encoding.UTF8.GetString(encoded.ToArray());
        }

        /// <summary>
        /// Header keys and values have to be decoded since they can contain escape sequences for literal values which collide with control words.
        /// Following escape sequences must be decoded to their original value: Colons, Line-Feed, Carriage Return + Line-Feed and the Backslash.
        /// </summary>
        /// <remarks>https://stomp.github.io/stomp-specification-1.2.html#Value_Encoding</remarks>
        public static string DecodeHeader(string value)
        {
            if (value.Length == 0)
            {
                return string.Empty;
            }

            var decoded = new List<byte>();
            var toDecode = Encoding.UTF8.GetBytes(value);
            var i = 1;
            int offset;
            for (; i < toDecode.Length; i += offset)
            {
                // offset; when a encoded character is found the offset is 2; otherwise 1.
                offset = 2;
                if (toDecode[i - 1] == ByteConstants.Backslash && toDecode[i] == ByteConstants.KeyR)
                {
                    decoded.Add(ByteConstants.CarriageReturn);
                }
                else if (toDecode[i - 1] == ByteConstants.Backslash && toDecode[i] == ByteConstants.KeyN)
                {
                    decoded.Add(ByteConstants.LineFeed);
                }
                else if (toDecode[i - 1] == ByteConstants.Backslash && toDecode[i] == ByteConstants.KeyC)
                {
                    decoded.Add(ByteConstants.Colon);
                }
                else if (toDecode[i - 1] == ByteConstants.Backslash && toDecode[i] == ByteConstants.Backslash)
                {
                    decoded.Add(ByteConstants.Backslash);
                }
                else if (toDecode[i - 1] == ByteConstants.Backslash)
                {
                    throw new Exceptions.StompException("fatal protocol error: Detected invalid escape sequence!");
                }
                else
                {
                    decoded.Add(toDecode[i - 1]);
                    offset = 1;
                }
            }

            if (i == toDecode.Length)
            {
                decoded.Add(toDecode[i - 1]);
            }

            return Encoding.UTF8.GetString(decoded.ToArray());
        }
    }
}
