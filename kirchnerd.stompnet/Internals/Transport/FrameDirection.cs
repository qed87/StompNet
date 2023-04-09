namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// Frames are distinguished according to their receiver. The possible values are recorded in this list of constants.
    /// </summary>
    public enum FrameType
    {
        Server,
        Client
    }

}