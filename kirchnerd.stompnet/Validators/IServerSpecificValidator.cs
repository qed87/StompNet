namespace kirchnerd.StompNet.Validators;

public interface IServerSpecificValidator
{
    void Validate(ValidationContext validationContext);
}