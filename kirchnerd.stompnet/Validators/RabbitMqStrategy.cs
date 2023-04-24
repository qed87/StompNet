using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals;

namespace kirchnerd.StompNet.Validators;

public class RabbitMqStrategy : ServerBaseStrategy, IServerSpecificValidator
{
    public override void Validate(ValidationContext validationContext)
    {
        if (!validationContext.IsRequest) return;
        var frame = validationContext.Frame;
        if (!frame.HasHeader(StompConstants.Headers.ReplyTo))
            throw new StompValidationException("Reply-to header expected for request frame with RabbitMq-Server!");
        var replyTo = frame.GetHeader(StompConstants.Headers.ReplyTo);
        if (!replyTo.StartsWith("/temp-queue/"))
            throw new StompValidationException("Reply-to header must target a temp-queue with RabbitMq-Server.");
    }
}