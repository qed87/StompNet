using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class ReceiptFrame : ServerStompFrame
    {
        public ReceiptFrame()
            : base(StompConstants.Commands.Receipt)
        {
        }
    }
}
