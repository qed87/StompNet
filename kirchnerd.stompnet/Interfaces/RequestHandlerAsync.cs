using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Transport.Frames;

namespace kirchnerd.StompNet.Interfaces;

public delegate Task<SendFrame> RequestHandlerAsync(MessageFrame frame, ISession messaging);