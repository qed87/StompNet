using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Strategies;

/// <summary>
/// Validation context used by <see cref="IServerSpecificValidator"/>.
/// </summary>
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