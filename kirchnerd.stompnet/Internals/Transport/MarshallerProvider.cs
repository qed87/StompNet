using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// Provider of marshallers based on the constitution of the input frame.
    /// </summary>
    internal class MarshallerProvider : IMarshallerProvider
    {
        public IMarshaller Get(StompFrame frame, ILogger<StompDriver> logger)
        {
            if (frame.Command == "HEARTBEAT")
            {
                return new ClientHeartBeatMarshaller();
            }

            return new StompMarshaller(logger);
        }
    }
}
