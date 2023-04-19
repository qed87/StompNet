using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Services;

public interface ISubscriptionService : IDisposable
{
    AcknowledgeMode GetAcknowledgeMode(string id);

    void NotifyListeners(StompFrame frame);

    bool AddListener(
        string id,
        ISession session,
        Func<StompFrame, string, AcknowledgeMode, Task> handleMessage,
        AcknowledgeMode acknowledgeMode);

    public IListenerDisposal AddReplyListener(Predicate<StompFrame> predicate);

    bool RemoveListener(string id);
}