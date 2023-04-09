using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class AckFrame : StompFrame
    {
        public AckFrame()
            : base(StompConstants.Commands.Ack, FrameType.Client, 5)
        {
        }
    }
}
