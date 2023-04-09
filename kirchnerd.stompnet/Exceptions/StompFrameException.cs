using System;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Exceptions
{
    /// <summary>
    /// An invalid frame exception.
    /// </summary>
    public class StompFrameException : StompException
    {
        internal StompFrameException(StompFrame frame, string errorMessage)
            : base(errorMessage)
        {
            Frame = frame;
        }

        internal StompFrameException(StompFrame frame, Exception exception)
            : base("Invalid Stomp Frame.", exception)
        {
            Frame = frame;
        }

        public StompFrame Frame { get; }
    }
}
