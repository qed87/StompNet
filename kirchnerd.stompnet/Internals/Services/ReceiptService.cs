using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace kirchnerd.StompNet.Internals.Services
{
    /// <summary>
    /// Handles the cumulative acknowledgment of receipts.
    /// </summary>
    internal sealed class ReceiptService : IReceiptService
    {
        private readonly object _sync = new();
        private readonly ConcurrentDictionary<string, Receipt> _receipts = new();
        private volatile int _isReceiptTimerRunning;
        private readonly Timer _timer;
        private long _lastReceipt;
        private bool _disposed;

        public ReceiptService()
        {
            _timer = new Timer(Run);
            _timer.Change(0, 100);
        }

        /// <summary>
        /// Periodically scans the registered receipts for cumulative acknowledgment.
        /// </summary>
        public void Run(object? state)
        {
            if (Interlocked.CompareExchange(ref _isReceiptTimerRunning, 1, 0) == 1)
            {
                return;
            }

            long lastReceiptReceivedInTicks;
            lock (_sync)
            {
                lastReceiptReceivedInTicks = _lastReceipt;
            }

            foreach (var eachReceipt in _receipts.ToArray())
            {
                if (eachReceipt.Value.Timestamp > lastReceiptReceivedInTicks) continue;
                if (_receipts.TryRemove(eachReceipt.Key, out _))
                {
                    eachReceipt.Value.CompletionSource.SetResult();
                }
            }

            Interlocked.Exchange(ref _isReceiptTimerRunning, 0);
        }

        /// <summary>
        /// Waits until the given receipt is received.
        /// </summary>
        /// <param name="receiptId">The receipt id.</param>
        public Task WaitForReceiptAsync(string receiptId)
        {
            var receipt = new Receipt();
            _receipts.TryAdd(receiptId, receipt);

            return receipt.CompletionSource.Task;
        }

        /// <summary>
        /// Updates an internal timestamp to the last received receipt timestamp since
        /// receipts are cumulative. All receipts sent before the timestamp are automatically confirmed.
        /// </summary>
        /// <param name="receiptId">The received receipt.</param>
        public void Receive(string receiptId)
        {
            if (!_receipts.TryGetValue(receiptId, out var receipt)) return;
            lock (_sync)
            {
                if (receipt.Timestamp > _lastReceipt)
                {
                    _lastReceipt = receipt.Timestamp;
                }
            }
        }

        public void TryRemove(string receiptId)
        {
            _receipts.TryRemove(receiptId, out _);
        }

        private class Receipt
        {
            public Receipt()
            {
                CompletionSource = new TaskCompletionSource();
                Timestamp = DateTimeOffset.UtcNow.Ticks;
            }

            public TaskCompletionSource CompletionSource { get; }

            public long Timestamp { get; }

        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _timer.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
