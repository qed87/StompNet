using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Strategies;

/// <summary>
/// Provider for the reply to header.
/// </summary>
public interface IReplyHeaderProvider
{
    string GetReplyHeader(SendFrame sendFrame);
}