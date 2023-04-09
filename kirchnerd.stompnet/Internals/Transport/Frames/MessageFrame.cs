using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class MessageFrame : ServerStompFrame
    {
        private bool? _acknowledged = true;

        public MessageFrame()
            : base(StompConstants.Commands.Message)
        {
        }

        public SendFrame ToSendFrame()
        {
            var sendFrame = new SendFrame();
            foreach (var headerName in GetHeaderNames())
            {
                if (string.Equals(headerName, StompConstants.Headers.ContentType))
                {
                    continue;
                }

                if (string.Equals(headerName, StompConstants.Headers.ContentLength))
                {
                    continue;
                }

                if (string.Equals(headerName, StompConstants.Headers.Subscription))
                {
                    continue;
                }

                if (string.Equals(headerName, StompConstants.Headers.Internal.Received))
                {
                    continue;
                }

                if (string.Equals(headerName, StompConstants.Headers.Internal.Sent))
                {
                    continue;
                }

                var value = sendFrame.GetHeader(headerName);

                sendFrame.SetHeader(headerName, value);
            }

            return sendFrame;
        }

        public void Ack()
        {
            _acknowledged = true;
        }

        public void Nack()
        {
            _acknowledged = false;
        }

        internal bool? IsAcknowledged()
        {
            return _acknowledged;
        }

        public SendFrame Reply()
        {
            var sendFrame = ToSendFrame();
            sendFrame.DeleteHeader(StompConstants.Headers.Destination);
            var replyDestination = sendFrame.GetHeader(StompConstants.Headers.ReplyTo);
            sendFrame.DeleteHeader(StompConstants.Headers.ReplyTo);
            sendFrame.WithDestination(replyDestination);
            sendFrame.ResetBody();
            return sendFrame;
        }
    }
}
