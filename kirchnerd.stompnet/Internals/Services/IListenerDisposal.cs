using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Services;

public interface IListenerDisposal : IDisposable
{
    Task<StompFrame> GetResult();
}