using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class AckFrame : StompFrame, IAcknowledge
    {
        public AckFrame()
            : base(StompConstants.Commands.Ack, FrameType.Client, 5)
        {
        }

        public string Id => GetHeader(StompConstants.Headers.AckId);
    }
}
