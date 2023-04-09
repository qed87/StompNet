using System.Threading;

namespace kirchnerd.StompNet.Internals.Transport
{
    public delegate byte FrameBytesRead(CancellationToken cancellationToken);
}