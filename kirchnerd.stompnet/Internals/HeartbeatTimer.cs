using System;
using System.Threading;

namespace kirchnerd.StompNet.Internals
{
    /// <summary>
    /// Timer which checks whether heartbeats where received in time or
    /// initiate outgoing heartbeats.
    /// </summary>
    internal sealed class HeartbeatTimer : IDisposable
    {
        private const int Infinite = -1;

        private long? _lastHeartbeat = DateTimeOffset.UtcNow.Ticks;
        private volatile int _isRunning;
        private readonly Timer _timer;
        private readonly Action _onElapsed;
        private long _lastFrameInTicks;
        private bool _disposed;

        public HeartbeatTimer(Action onElapsed)
        {
            _onElapsed = onElapsed;
            _timer = new Timer(Run);
        }

        public void Start(long interval)
        {
            _timer.Change(0, interval);
        }

        public void Stop()
        {
            _timer.Change(0, Infinite);
        }

        private void Run(object? state)
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            {
                return;
            }

            var value = Interlocked.Read(ref _lastFrameInTicks);
            var delta = value - _lastHeartbeat;
            if (delta < 0)
            {
                _onElapsed();
            }

            _lastHeartbeat = DateTimeOffset.UtcNow.Ticks;
            Interlocked.Exchange(ref _isRunning, 0);
        }

        public void Update()
        {
            Interlocked.Exchange(ref _lastFrameInTicks, DateTimeOffset.UtcNow.Ticks);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _timer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
