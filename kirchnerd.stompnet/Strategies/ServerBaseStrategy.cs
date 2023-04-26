using kirchnerd.StompNet.Internals;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Strategies;

/// <summary>
/// Base strategy for server specific strategies.
/// </summary>
public abstract class ServerBaseStrategy : IServerSpecificValidator, IReplyHeaderProvider
{
    public virtual string GetReplyHeader(SendFrame sendFrame)
    {
        return sendFrame.GetHeader(StompConstants.Headers.ReplyTo);
    }

    public abstract void Validate(ValidationContext validationContext);
}