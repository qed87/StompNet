using System;
using kirchnerd.StompNet.Exceptions;

namespace kirchnerd.StompNet.Internals.Services;

internal class InboxHeartbeatService : HeartbeatServiceBase
{
    private readonly StompClient _stompClient;

    public InboxHeartbeatService(StompClient stompClient)
    {
        _stompClient = stompClient;
    }

    protected override void OnElapsed()
    {
        _stompClient.OnError(new StompHeartbeatMissingException());
    }
}