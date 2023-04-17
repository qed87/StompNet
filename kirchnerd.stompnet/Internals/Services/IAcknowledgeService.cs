using System;

namespace kirchnerd.StompNet.Internals.Services;

internal interface IAcknowledgeService : IDisposable
{
    public void Register(string ackId, string subscriptionId, long ticks);

    public bool IsAcknowledgeRequired(string ackId);
}