using System;
using System.Threading.Tasks;

namespace kirchnerd.StompNet.Internals.Services;

internal interface IReceiptService : IDisposable
{
    /// <summary>
    /// Waits until the given receipt is received.
    /// </summary>
    /// <param name="receiptId">The receipt id.</param>
    public Task WaitForReceiptAsync(string receiptId);

    /// <summary>
    /// Updates an internal timestamp to the last received receipt timestamp since
    /// receipts are cumulative. All receipts sent before the timestamp are automatically confirmed.
    /// </summary>
    /// <param name="receiptId">The received receipt.</param>
    public void Receive(string receiptId);

    public void TryRemove(string receiptId);
}