using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class NackFrame : StompFrame, IAcknowledge
    {
        public NackFrame()
            : base(StompConstants.Commands.Nack, FrameType.Client, 5)
        {
        }

        public string Id => GetHeader(StompConstants.Headers.AckId);
    }
}
