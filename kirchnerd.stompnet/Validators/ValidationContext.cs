using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Validators;

public class ValidationContext
{
    public ValidationContext(SendFrame frame, bool isRequest)
    {
        Frame = frame;
        IsRequest = isRequest;
    }

    public SendFrame Frame { get; }

    public bool IsRequest { get; }
}