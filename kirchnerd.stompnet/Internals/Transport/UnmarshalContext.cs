using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using kirchnerd.StompNet.Exceptions;

namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// This helper class is used by the parser (unmarshaller). It keep's track of bytes already read.
    /// </summary>
    internal class UnmarshalContext
    {
        private readonly CancellationToken _cancellationToken;
        private readonly FrameBytesRead _readByte;
        private MemoryStream _buffer;

        public UnmarshalContext(FrameBytesRead readByte, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _readByte = readByte;
            _buffer = new MemoryStream();
        }

        /// <summary>
        /// Consumes the current line including the EOL but only returns the payload.
        /// </summary>
        /// <returns></returns>
        public byte[] ReadLine()
        {
            var octets = new List<byte>();
            do
            {
                var octet = ReadByte();
                if (octet == ByteConstants.LineFeed)
                {
                    if (octets.Count > 0 && octets[^1] == ByteConstants.CarriageReturn)
                    {
                        // remove the previous read byte from the result since this also belongs
                        // to the control sequence...
                        octets.RemoveAt(octets.Count - 1);
                    }

                    break;
                }

                octets.Add(octet);
            }
            while (true);

            return octets.ToArray();
        }

        public byte ReadByte()
        {
            if (_buffer.Position < _buffer.Length)
            {
                // reads the byte from the buffer since it's cursor is positioned before the buffer end...
                return (byte)_buffer.ReadByte();
            }

            // only reads a byte from the network stream if no more buffered bytes are available...
            var octet = _readByte(_cancellationToken);

            _buffer.WriteByte(octet);
            return octet;
        }

        /// <summary>
        /// Reads the next {length} bytes starting at {offset} and copies the read bytes to a buffer.
        /// </summary>
        public void ReadBytes(byte[] bytes, int offset, int length)
        {
            var octets = new byte[length];
            for (int n = length, i = 0; n > 0; n--, i++)
            {
                var octet = ReadByte();
                octets[i] = octet;
            }

            Array.Copy(octets, 0, bytes, offset, length);
        }

        /// <summary>
        /// Consumes the next bytes if an EOL is present. Otherwise, no bytes are consumed.
        /// </summary>
        /// <return>True, if bytes were consumed. Otherwise false.</return>
        public bool IsEndOfLine()
        {
            var octet = ReadByte();
            ReturnByte(); // immediately return read byte since it's value is temporary stored inside octet
            if (octet != ByteConstants.CarriageReturn && octet != ByteConstants.LineFeed)
            {
                return false;
            }

            if (octet != ByteConstants.CarriageReturn) return true;
            octet = ReadByte();
            ReturnByte(); // immediately return read byte since it's value is temporary stored inside octet
            return octet == ByteConstants.LineFeed;
        }

        public void ReadEndOfLine()
        {
            var octet = ReadByte();
            switch (octet)
            {
                case ByteConstants.LineFeed:
                    return;
                case ByteConstants.CarriageReturn:
                {
                    octet = ReadByte();
                    if (octet == ByteConstants.LineFeed)
                    {
                        return;
                    }

                    break;
                }
            }

            throw new StompException("Parser exception: expected EOL but found something else");
        }

        public void ReturnByte()
        {
            ReturnBytes(1);
        }

        /// <summary>
        /// Modify the buffer cursor so that the cursor stands before end of the buffer. Subsequent Read() calls will first by served from the buffer.
        /// </summary>
        public void ReturnBytes(int n)
        {
            switch (n)
            {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(n));
                case 0:
                    return;
                default:
                    _buffer.Seek(-1 * n, SeekOrigin.Current);
                    break;
            }
        }
    }
}
