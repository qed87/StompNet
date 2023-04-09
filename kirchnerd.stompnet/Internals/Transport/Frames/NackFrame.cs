using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class NackFrame : StompFrame
    {
        public NackFrame()
            : base(StompConstants.Commands.Nack, FrameType.Client, 10)
        {
        }
    }
}
