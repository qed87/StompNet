using System.Collections.Generic;
using System.Linq;
using kirchnerd.StompNet.Exceptions;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class ConnectFrame : StompFrame
    {
        public ConnectFrame()
            : base(StompConstants.Commands.Stomp, FrameType.Client, 10)
        {
        }

        public override void Validate()
        {
            List<string> failures = new ();
            if (!HasHeader(StompConstants.Headers.AcceptVersion))
            {
                failures.Add($"Header '{StompConstants.Headers.AcceptVersion}' must not be empty.");
            }

            if (!HasHeader(StompConstants.Headers.Host))
            {
                failures.Add($"Header '{StompConstants.Headers.Host}' must not be empty.");
            }

            if (failures.Any()) throw new StompValidationException(failures.ToArray());
        }
    }
}
