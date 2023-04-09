using System;

namespace kirchnerd.StompNet.Extensions
{
    internal static class AcknowledgeModeExtensions
    {
        public static string ToHeaderValue(this AcknowledgeMode mode)
        {
            return mode switch
            {
                AcknowledgeMode.Auto => "auto",
                AcknowledgeMode.Client => "client",
                AcknowledgeMode.ClientIndividual => "client-individual",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode.ToString())
            };
        }
    }
}
