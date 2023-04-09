using System.Threading;
using System.Threading.Tasks;

namespace kirchnerd.StompNet.Extensions
{
    public static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken @this)
        {
            var tcs = new TaskCompletionSource();
            @this.Register(() => tcs.SetResult());
            return tcs.Task;
        }
    }
}
