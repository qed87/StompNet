using System;

namespace kirchnerd.StompNet.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a heartbeat is missing.
    /// </summary>
    [Serializable]
    internal class StompHeartbeatMissingException : StompException
    {
        public StompHeartbeatMissingException()
            : base("Does not received heartbeat in time!")
        {
        }
    }
}