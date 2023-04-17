using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Services;

public class AcknowledgeService : IAcknowledgeService
{
    private long _lastAck;
    private readonly ILogger<StompDriver> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ConcurrentDictionary<string, Acknowledgement> _acknowledged = new();

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

    public bool IsAcknowledgeRequired(string ackId)
    {
        if (!_acknowledged.TryRemove(ackId, out var acknowledgment))
        {
            return false;
        }

        var acknowledgeMode = _subscriptionService.GetAcknowledgeMode(acknowledgment.SubscriptionId);
        switch (acknowledgeMode)
        {
            case AcknowledgeMode.Auto:
                _logger.LogDebug(
                    StompEventIds.StompClient,
                    $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' is in mode 'auto'.");
                return false;
            case AcknowledgeMode.Client when acknowledgment.Timestamp <= _lastAck:
                _logger.LogDebug(
                    StompEventIds.StompClient,
                    $"Skip ack/nack since subscription with Id='{acknowledgment.SubscriptionId}' "
                    + "is in mode 'client' and a more recent message frame is already acknowledged.");
                return false;
            case AcknowledgeMode.ClientIndividual:
            default:
                _logger.LogTrace(
                    StompEventIds.StompClient,
                    $"Update acknowledge timestamp.");
                Interlocked.Exchange(ref _lastAck, acknowledgment.Timestamp);
                break;
        }

        return true;
    }

    public void Dispose()
    {
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
}