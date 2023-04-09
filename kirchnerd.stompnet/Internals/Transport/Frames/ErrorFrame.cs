using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class ErrorFrame : ServerStompFrame
    {
        public ErrorFrame()
            : base(StompConstants.Commands.Error)
        {
        }
    }
}
