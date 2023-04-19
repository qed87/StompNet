using System;

namespace kirchnerd.StompNet.Internals.Services;

internal interface IHeartbeatService : IDisposable
{
    public void Start(long interval);

    public void Stop();

    public void Update();
}