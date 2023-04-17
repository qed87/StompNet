using System.Threading;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Middleware;

public record OutboxContext(StompFrame Frame, CancellationToken CancellationToken);
