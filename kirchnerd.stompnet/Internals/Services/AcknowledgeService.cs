using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Services;

public class AcknowledgeService : IAcknowledgeService
{
    private readonly ILogger<StompDriver> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ConcurrentDictionary<string, Acknowledgement> _acknowledged = new();
    private readonly Dictionary<string, long> _lastAckNackPerSubscription = new();

    public AcknowledgeService(ILogger<StompDriver> logger, ISubscriptionService subscriptionService)
    {
        _logger = logger;
        _subscriptionService = subscriptionService;
    }

    public void Register(string ackId, string subscriptionId, long ticks)
    {
        var acknowledgment = new Acknowledgement(
            subscriptionId,
            ticks);
        _acknowledged.TryAdd(
            ackId,
            acknowledgment);
    }

    public bool IsAcknowledgeRequired(string ackId, out string? subscriptionId, out long? timestamp)
    {
        subscriptionId = null;
        timestamp = null;
        if (!_acknowledged.TryRemove(ackId, out var acknowledgment))
        {
            return false;
        }

        subscriptionId = acknowledgment.SubscriptionId;
        timestamp = acknowledgment.Timestamp;

        // ack must be tracked per subscription
        var acknowledgeMode = _subscriptionService.GetAcknowledgeMode(acknowledgment.SubscriptionId);
        if (acknowledgeMode is AcknowledgeMode.ClientIndividual) return true;
        switch (acknowledgeMode)
        {
            case AcknowledgeMode.Auto:
                _logger.LogDebug(
                    StompEventIds.AcknowledgeService,
                    $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' is in mode 'auto'.");
                return false;
            case AcknowledgeMode.Client when acknowledgment.Timestamp <= _lastAckNackPerSubscription.GetValueOrDefault(acknowledgment.SubscriptionId):
                _logger.LogDebug(
                    StompEventIds.AcknowledgeService,
                    $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' "
                    + "is in mode 'client' and a more recent message frame is already acknowledged.");
                return false;
        }

        return true;
    }

    public void Update(string subscriptionId, long timestamp)
    {
        _logger.LogTrace(
            StompEventIds.AcknowledgeService,
            $"Update acknowledge timestamp.");
        _lastAckNackPerSubscription[subscriptionId] = timestamp;
    }

    private class Acknowledgement
    {
        public Acknowledgement(string subscriptionId, long receivedTicks)
        {
            Timestamp = receivedTicks;
            SubscriptionId = subscriptionId;
        }

        public string SubscriptionId { get; }

        public long Timestamp { get; }
    }

    public void Dispose()
    {
    }
}