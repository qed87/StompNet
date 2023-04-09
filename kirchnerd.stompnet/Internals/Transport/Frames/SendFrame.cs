using System;
using System.Collections.Generic;
using System.Linq;
using kirchnerd.StompNet.Exceptions;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class SendFrame : StompFrame
    {
        private static readonly SendFrame NullFrame = new();

        public SendFrame()
            : base(StompConstants.Commands.Send, FrameType.Client, 10)
        {
        }

        internal void WithDestination(string destination)
        {
            DeleteHeader(StompConstants.Headers.Destination);
            SetHeader(StompConstants.Headers.Destination, destination);
        }

        public void ReplyTo(string destination)
        {
            DeleteHeader(StompConstants.Headers.ReplyTo);
            SetHeader(
                StompConstants.Headers.ReplyTo,
                destination.StartsWith("/temp-queue/")
                ? destination
                : $"/temp-queue/{destination}");
        }

        public void WithReceipt()
        {
            DeleteHeader(StompConstants.Headers.Receipt);
            SetHeader(StompConstants.Headers.Receipt, Guid.NewGuid().ToString().Replace("-", ""));
        }

        public override void Validate()
        {
            List<string> failures = new ();
            if (!HasHeader(StompConstants.Headers.Destination))
            {
                failures.Add($"Header '{StompConstants.Headers.Destination}' must not be empty.");
            }

            if (!HasHeader(StompConstants.Headers.ContentType))
            {
                failures.Add($"Header '{StompConstants.Headers.ContentType}' must not be empty.");
            }

            if (failures.Any()) throw new StompValidationException(failures.ToArray());
        }

        public static SendFrame Void()
        {
            return NullFrame;
        }
    }
}
