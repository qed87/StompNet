using System.Threading;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Services;

internal class OutboxHeartbeatService : HeartbeatServiceBase
{
    private readonly StompClient _stompClient;

    public OutboxHeartbeatService(StompClient stompClient)
    {
        _stompClient = stompClient;
    }

    protected override void OnElapsed()
    {
        _stompClient.Send(StompFrame.CreateHeartbeat(FrameType.Client), CancellationToken.None);
    }
}