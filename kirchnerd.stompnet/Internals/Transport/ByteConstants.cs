namespace kirchnerd.StompNet.Internals.Transport
{
    /// <summary>
    /// Common byte constants which are used while marshalling or un-marshalling frames
    /// </summary>
    internal static class ByteConstants
    {
        public const byte CarriageReturn = 0x0d;
        public const byte LineFeed = 0x0a;
        public const byte KeyC = 0x63;
        public const byte KeyN = 0x6e;
        public const byte KeyR = 0x72;
        public const byte Colon = 0x3a;
        public const byte Backslash = 0x5c;
        public const byte Null = 0x00;
    }
}
