using System;
using System.Collections.Generic;

namespace kirchnerd.StompNet.Exceptions
{
    /// <summary>
    /// Stomp Validation exception.
    /// </summary>
    [Serializable]
    internal class StompValidationException : StompException
    {
        public StompValidationException(string message)
            : base(message)
        {
        }

        public StompValidationException(IEnumerable<string> errors)
            : this(string.Join(Environment.NewLine, errors))
        {
        }
    }
}