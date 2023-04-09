using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Interfaces
{
    public delegate void FrameHandler(MessageFrame frame);
    internal delegate void FrameHandlerInternal(StompFrame frame);
    public delegate Task<SendFrame> FrameHandlerAsync(MessageFrame frame, ISession messaging);
    internal delegate Task<SendFrame> FrameHandlerInternalAsync(StompFrame frame);
}