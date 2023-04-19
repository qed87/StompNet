using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ILogger<StompDriver> _logger;
    private readonly ConcurrentDictionary<string, Listener> _listeners = new();

    public SubscriptionService(ILogger<StompDriver> logger)
    {
        _logger = logger;
    }

    public AcknowledgeMode GetAcknowledgeMode(string id)
    {
        return !_listeners.TryGetValue(id, out var listener) || listener is not SubscriptionListener subscriptionListener
            ? AcknowledgeMode.Auto
            : subscriptionListener.SubscriptionMode;
    }

    /// <summary>
    /// Detects custom handlers which are interested at the given frame
    /// and starts a worker to invoke them.
    /// </summary>
    /// <param name="frame">The received frame.</param>
    public async void NotifyListeners(StompFrame frame)
    {
        var handlerTasks = new List<Task>();
        foreach (var (_, listener) in _listeners.ToArray())
        {
            if (!listener.Check(frame))
            {
                continue;
            }

            handlerTasks.Add(Task.Run(() => listener.Handle(frame)));
        }

        await Task.WhenAll(handlerTasks);
    }

    public bool AddListener(
        string id,
        ISession session,
        Func<StompFrame, string, AcknowledgeMode, Task> handleMessage,
        AcknowledgeMode acknowledgeMode)
    {
        var listener = new SubscriptionListener(
            frame =>
                string.Equals(frame.Command, StompConstants.Commands.Message, StringComparison.OrdinalIgnoreCase) &&
                frame.HasHeader(StompConstants.Headers.SubscriptionId) &&
                frame.GetHeader(StompConstants.Headers.SubscriptionId) == id,
            handleMessage,
            id,
            acknowledgeMode);
        return _listeners.TryAdd(id, listener);
    }

    public IListenerDisposal AddReplyListener(Predicate<StompFrame> predicate)
    {
        var listenerId = Guid.NewGuid().ToString().Replace("-", "");
        if (!_listeners.TryAdd(
                listenerId,
                new ReplyListener(predicate)))
            throw new InvalidOperationException("Error while registering a listener.");
        return new ListenerDisposal(this, listenerId);
    }

    public bool RemoveListener(string id)
    {
        return _listeners.TryRemove(id, out _);
    }

    public void Dispose()
    {
    }

    private abstract class Listener
    {
        protected Listener(
            Predicate<StompFrame> predicate)
        {
            Check = predicate;
        }

        public Predicate<StompFrame> Check { get; }

        public abstract Task Handle(StompFrame frame);
    }

    private class SubscriptionListener : Listener
    {
        private Func<StompFrame, string, AcknowledgeMode, Task> _handler;
        public string SubscriptionId { get; }

        public AcknowledgeMode SubscriptionMode { get; }

        public SubscriptionListener(
            Predicate<StompFrame> predicate,
            Func<StompFrame, string, AcknowledgeMode, Task> handler,
            string subscriptionId,
            AcknowledgeMode subscriptionMode)
            : base(predicate)
        {
            _handler = handler;
            SubscriptionId = subscriptionId;
            SubscriptionMode = subscriptionMode;
        }

        public override async Task Handle(StompFrame frame)
        {
            await _handler(frame, SubscriptionId, SubscriptionMode);
        }
    }

    private class ReplyListener : Listener
    {
        public readonly TaskCompletionSource<StompFrame> TaskCompletionSource = new();

        public ReplyListener(Predicate<StompFrame> predicate)
            : base(predicate)
        {
        }

        public override Task Handle(StompFrame frame)
        {
            TaskCompletionSource.TrySetResult(frame);
            return Task.CompletedTask;
        }
    }

    private class ListenerDisposal : IListenerDisposal
    {
        private readonly SubscriptionService _subscription;
        private readonly string _listenerId;

        public ListenerDisposal(SubscriptionService subscription, string listenerId)
        {
            _subscription = subscription;
            _listenerId = listenerId;
        }

        public void Dispose()
        {
            _subscription.RemoveListener(_listenerId);
        }

        public async Task<StompFrame> GetResult()
        {
            if (!_subscription._listeners.TryGetValue(_listenerId, out var listener) || listener is not ReplyListener replyListener)
                throw new InvalidOperationException($"Listener '{_listenerId}' is already removed.");

            return await replyListener.TaskCompletionSource.Task;
        }
    }
}