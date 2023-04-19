using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals.Interfaces;
using kirchnerd.StompNet.Internals.Middleware;
using kirchnerd.StompNet.Internals.Services;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// This class represents the outbox of the driver component and holds a queue of messages destined to the broker.
    /// </summary>
    /// <remarks>
    /// Only one thread may write to the socket connection at a time but a simultaneously read is allowed.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Manual reset event is used correctly.")]
    internal sealed class StompOutbox : IDisposable
    {
        public event ErrorHandler? Error;

        private void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }

        private const int MaxMessageSize = 125829120;

        private readonly PriorityQueue<(StompFrame Frame, CancellationToken cancellationToken, Action OnCompleted, Action<Exception> OnError), int> _queue = new();

        private readonly ManualResetEventSlim _manualResetEventSlim = new(false);

        private readonly CancellationToken _cancelToken;

        private readonly object _sync = new();

        private readonly ILogger<StompDriver> _logger;

        private readonly string _connectionString;

        private volatile bool _isRunning = true;

        private readonly IMarshallerProvider _provider;

        private Pipeline<OutboxContext> _writePipeline;

        public StompOutbox(string connectionString,
            ILogger<StompDriver> logger,
            IMarshallerProvider provider,
            Pipeline<OutboxContext> writePipeline,
            CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            _connectionString = connectionString;
            _writePipeline = writePipeline;
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new frame to the queue. These frames are destined for the message broker and sent frame by frame.
        /// </summary>
        /// <param name="frame">The stomp frame to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task EnqueueAsync(StompFrame frame, CancellationToken cancellationToken)
        {
            if (!_isRunning)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource();
            lock (_sync)
            {
                _queue.Enqueue((frame, cancellationToken, () => tcs.TrySetResult(), err => tcs.TrySetException(err)), frame.Priority);
                _manualResetEventSlim.Set();
                _logger.LogTrace(
                    StompEventIds.Outbox,
                    $"Enqueued message on Outbox. Queue Length {_queue.Count} on '{_connectionString}'.");
            }

            return tcs.Task;
        }

        /// <summary>
        /// This method represents the entry point for the thread which is responsible for writing messages to the broker.
        /// </summary>
        /// <remarks>
        /// Implementation of a Producer-Consumer scenario.
        /// </remarks>
        public void Run()
        {
            try
            {
                do
                {
                    // wait for new frames
                    _manualResetEventSlim.Wait(_cancelToken);

                    // check whether cancellation is requested
                    if (_cancelToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // (StompFrame Frame, CancellationToken CancellationToken, Action OnCompleted, Action<Exception> OnError) entry;
                    int priority;

                    // Critical section: Enqueue() and mrs.Set() must be executed mutually exclusive to Dequeue() and mrs.Reset().
                    // EXAMPLE: Otherwise it could happen that an enqueued item is not processed since mrs.Reset() is called after mrs.Set().
                    StompFrame frame;
                    CancellationToken cancellationToken;
                    Action onCompleted;
                    Action<Exception> onError;
                    lock (_sync)
                    {
                        // try to get the next frame
                        if (!_queue.TryDequeue(out var entry, out priority))
                        {
                            // If no frames are available reset the manual reset event.
                            // This has the consequence that the next call of wait is blocked,
                            // if a new frame is not received in the meantime
                            _manualResetEventSlim.Reset();
                            continue;
                        }

                        // next frame for processing available...
                        (frame, cancellationToken, onCompleted, onError) = entry;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        onError(new TimeoutException());
                        continue;
                    }

                    if (frame.Type == FrameType.Server)
                    {
                        // if the frame is not destined for the broker something went totally wrong...
                        _logger.LogError(
                            StompEventIds.Outbox,
                            $"Frame intended for the client was mistakenly routed to the STOMP Outbox Agent on '{_connectionString}':\r\n\r\n{frame}");
                        // inform the sender about this mistake.
                        onError(new StompFrameException(frame, "Frame is not intended for stomp server; frame discarded from outbox!"));
                        continue;
                    }

                    // going to send client frames to broker...
                    _logger.LogDebug(
                        StompEventIds.Outbox,
                        $"Send frame with Priority={priority} to STOMP-Server on '{_connectionString}':\r\n\r\n{frame}");

                    frame.MarkSend();
                    var marshaller = _provider.Get(frame, _logger);
                    var frameBytes = marshaller.Marshal(frame);
                    if (frameBytes.Length > MaxMessageSize)
                    {
                        _logger.LogCritical(
                            StompEventIds.Outbox,
                            $"Message is too big and hence disposed on '{_connectionString}':\r\n\r\n{frame.ToString(withBody: true)}");
                        // inform sender about message size violation.
                        onError(new StompFrameException(frame, "Frame exceeded max message size; frame disposed!"));
                        continue;
                    }

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // send frame on wire.
                        _writePipeline.ExecuteAsync(new OutboxContext(frame, frameBytes)).GetAwaiter().GetResult();

                        // inform sender that frame is sent.
                        onCompleted();
                    }
                    catch (Exception ex)
                    {
                        // inform sender about error.
                        onError(ex);
                    }
                }
                while (_isRunning);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    StompEventIds.Outbox,
                    $"Exception during write:\r\n\r\n{ex}");
                Stop();
                OnError(ex);
            }
        }

        public void Stop()
        {
            _logger.LogInformation(
                StompEventIds.Outbox,
                $"Stop Outbox requested on '{_connectionString}'.");
            _isRunning = false;
        }

        #region IDisposable Support

        private bool _dispose = false;

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_dispose) return;
            if (disposing)
            {
                // dispose managed resources here
                _manualResetEventSlim.Dispose();
            }

            // release unmanaged resources here
            _dispose = true;
        }
        #endregion IDisposable Support
    }
}
