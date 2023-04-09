using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class UnsubscribeFrame : StompFrame
    {
        public UnsubscribeFrame()
            : base(StompConstants.Commands.Unsubscribe, FrameType.Client, 5)
        {
        }

        internal void WithReceipt()
        {
            DeleteHeader(StompConstants.Headers.Receipt);
            SetHeader(StompConstants.Headers.Receipt, Guid.NewGuid().ToString().Replace("-", ""));
        }
    }
}
