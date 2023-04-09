using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class DisconnectFrame : StompFrame
    {
        public DisconnectFrame()
            : base(StompConstants.Commands.Disconnect, FrameType.Client, 10)
        {
        }

        public void WithReceipt()
        {
            DeleteHeader(StompConstants.Headers.Receipt);
            SetHeader(StompConstants.Headers.Receipt, Guid.NewGuid().ToString().Replace("-", ""));
        }
    }
}
