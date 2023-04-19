using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Middleware;

public record InboxContext (StompFrame Frame);