using kirchnerd.StompNet.Internals;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Validators;

public abstract class ServerBaseStrategy : IServerSpecificValidator, IReplyHeaderProvider
{
    public virtual string GetReplyHeader(SendFrame sendFrame)
    {
        return sendFrame.GetHeader(StompConstants.Headers.ReplyTo);
    }

    public abstract void Validate(ValidationContext validationContext);
}