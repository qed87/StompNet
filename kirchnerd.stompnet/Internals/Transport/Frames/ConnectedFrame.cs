using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using kirchnerd.StompNet.Exceptions;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class ConnectedFrame : ServerStompFrame
    {
        public ConnectedFrame()
            : base(StompConstants.Commands.Connected)
        {
        }

        public override void Validate()
        {
            List<string> failures = new ();
            if (!HasHeader(StompConstants.Headers.Version))
            {
                failures.Add($"Header '{StompConstants.Headers.Version}' must not be empty.");
            }

            if (!HasHeader(StompConstants.Headers.HeartBeat))
            {
                failures.Add($"Header '{StompConstants.Headers.HeartBeat}' must not be empty.");
            }

            if (!HasHeader(StompConstants.Headers.Session))
            {
                failures.Add($"Header '{StompConstants.Headers.Session}' must not be empty.");
            }

            if (failures.Any()) throw new StompValidationException(failures.ToArray());
        }
    }
}
