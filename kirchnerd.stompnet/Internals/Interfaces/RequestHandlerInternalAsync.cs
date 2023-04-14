using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Internals.Interfaces;

internal delegate Task<StompFrame> RequestHandlerInternalAsync(StompFrame frame);