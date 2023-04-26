namespace kirchnerd.StompNet.Strategies;

/// <summary>
///Validator for send and request frames.
/// </summary>
public interface IServerSpecificValidator
{
    void Validate(ValidationContext validationContext);
}