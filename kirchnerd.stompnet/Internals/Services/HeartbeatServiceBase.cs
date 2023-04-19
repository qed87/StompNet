using System;
using System.Threading;

namespace kirchnerd.StompNet.Internals.Services
{
    /// <summary>
    /// Timer which checks whether heartbeats where received in time or
    /// initiate outgoing heartbeats.
    /// </summary>
    internal abstract class HeartbeatServiceBase : IHeartbeatService
    {
        private const int Infinite = -1;

        private long? _lastHeartbeat = DateTimeOffset.UtcNow.Ticks;
        private volatile int _isRunning;
        private readonly Timer _timer;
        private long _lastFrameInTicks;
        protected bool Disposed;

        protected HeartbeatServiceBase()
        {
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
                OnElapsed();
            }

            _lastHeartbeat = DateTimeOffset.UtcNow.Ticks;
            Interlocked.Exchange(ref _isRunning, 0);
        }

        protected abstract void OnElapsed();

        public void Update()
        {
            Interlocked.Exchange(ref _lastFrameInTicks, DateTimeOffset.UtcNow.Ticks);
        }

        protected void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                _timer.Dispose();
            }

            Disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
