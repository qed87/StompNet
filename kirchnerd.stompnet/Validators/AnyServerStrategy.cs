using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Validators;

public class AnyServerStrategy : ServerBaseStrategy, IServerSpecificValidator
{
    public override void Validate(ValidationContext validationContext)
    {
    }
}