using System;

namespace kirchnerd.StompNet.Exceptions
{
    /// <summary>
    /// Base exception for Stomp.net.
    /// </summary>
    public class StompException : Exception
    {
        public StompException(string message)
            : base(message)
        {
        }

        public StompException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}
