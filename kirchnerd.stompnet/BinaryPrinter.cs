using System;
using System.Text;

namespace kirchnerd.StompNet
{
    public static class BinaryPrinter
    {
        /// <summary>
        /// Converts a binary payload to a hex representation.
        /// </summary>
        /// <param name="octets">The bytes to convert.</param>
        /// <returns>A hex string.</returns>
        public static string GetHex(byte[] octets)
        {
            var sb = new StringBuilder();
            if (octets.Length <= 0)
                return sb.ToString();
            var offset = 0;
            do
            {
                var segment = new byte[Math.Min(octets.Length - offset, 64)];
                Array.Copy(octets, offset, segment, 0, segment.Length);
                var hex = BitConverter.ToString(segment).Replace("-", " ");
                sb.AppendLine(hex);
                offset += 64;
            }
            while (offset < octets.Length);

            return sb.ToString();
        }
    }
}
