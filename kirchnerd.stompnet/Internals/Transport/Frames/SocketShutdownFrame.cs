using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class SocketShutdownFrame : ServerStompFrame
    {
        public SocketShutdownFrame()
            : base(StompConstants.Commands.Undefined)
        {
        }
    }
}
