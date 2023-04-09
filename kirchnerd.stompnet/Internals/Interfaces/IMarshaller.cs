using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Interfaces
{
    /// <summary>
    /// This is interface is used by clients which want to convert
    /// a <see href="StompWireFrame" /> to a binary representation 
    /// which can be transmitted over the network wire.
    /// </summary>
    interface IMarshaller
    {
        byte[] Marshal(StompFrame frame);
    }
}
