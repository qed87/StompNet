using System;

namespace kirchnerd.StompNet.Internals.Transport.Frames
{
    public class CommitFrame : StompFrame
    {
        public CommitFrame()
            : base(StompConstants.Commands.Commit, FrameType.Client, 10)
        {
        }
    }
}
