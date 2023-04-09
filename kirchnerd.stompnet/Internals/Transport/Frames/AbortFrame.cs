using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class AbortFrame : StompFrame
    {
        public AbortFrame()
            : base(StompConstants.Commands.Abort, FrameType.Client, 10)
        {
        }
    }
}
