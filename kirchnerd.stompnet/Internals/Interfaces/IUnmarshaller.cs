using System.Threading;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Interfaces
{
    /// <summary>
    /// This is interface is used by clients which want to convert
    /// a binary representation received over the network wire to 
    /// a <see href="StompWireFrame" />.
    /// </summary>
    interface IUnmarshaller
    {
        /// <summary>
        /// Reads the received frame from the wire using the provided delegate to read the next byte.
        /// </summary>
        /// <param name="bytesRead">A delegate which reads byte-by-byte from the network stream</param>
        /// <returns></returns>
        StompFrame Unmarshal(FrameBytesRead bytesRead, CancellationToken cancellationToken);
    }
}
