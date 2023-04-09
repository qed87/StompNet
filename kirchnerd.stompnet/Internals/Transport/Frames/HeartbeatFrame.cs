using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class HeartbeatFrame : StompFrame
    {
        public HeartbeatFrame(FrameType frameType)
            : base(StompConstants.Commands.Heartbeat, frameType, 0)
        {
        }
    }
}
