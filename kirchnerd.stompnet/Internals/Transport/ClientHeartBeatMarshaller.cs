using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// Special case marshaller that retrieves the bytes from a heart beat message.
    /// </summary>
    internal class ClientHeartBeatMarshaller : IMarshaller
    {
        public byte[] Marshal(StompFrame frame)
        {
            return new[] { ByteConstants.Null };
        }
    }
}
