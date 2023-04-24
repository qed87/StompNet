using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Validators;

public interface IReplyHeaderProvider
{
    string GetReplyHeader(SendFrame sendFrame);
}