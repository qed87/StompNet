using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class SubscribeFrame : StompFrame
    {
        public SubscribeFrame()
            : base(StompConstants.Commands.Subscribe, FrameType.Client, 5)
        {
        }

        internal void WithReceipt()
        {
            DeleteHeader(StompConstants.Headers.Receipt);
            SetHeader(StompConstants.Headers.Receipt, Guid.NewGuid().ToString().Replace("-", ""));
        }
    }
}
