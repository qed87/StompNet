using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;

// TODO: Apply Interface Segregation Principle (ISP)
namespace kirchnerd.StompNet.Internals.Interfaces
{
    internal delegate void FrameHandlerInternal(StompFrame frame);
    public delegate Task<SendFrame> FrameHandlerAsync(MessageFrame frame, ISession messaging);
}