namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public abstract class ServerStompFrame : StompFrame
    {
        protected ServerStompFrame(string command)
            : base(command, FrameType.Server, 10)
        {
        }
    }
}