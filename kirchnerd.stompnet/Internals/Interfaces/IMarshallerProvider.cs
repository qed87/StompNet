using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Interfaces
{
    /// <summary>
    /// A provider of <see href="IMarshaller" /> instances which
    /// is providing a implementation based on the handed in frame.
    /// </summary>
    internal interface IMarshallerProvider
    {
        IMarshaller Get(StompFrame frame, ILogger<StompDriver> logger);
    }
}
