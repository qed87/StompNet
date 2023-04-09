namespace kirchnerd.StompNet
{
    /// <summary>
    /// Protocol-related acknowledgement modes.
    /// This setting is used to report the consumption or non-consumption of messages back to the broker
    /// and is sent to the broker at subscription time.
    /// <see href="https://stomp.github.io/stomp-specification-1.2.html#SUBSCRIBE_ack_Header" />
    /// </summary>
    public enum AcknowledgeMode
    {
        /// <summary>
        /// When the ack mode is auto, then the client does not need to send the server ACK frames for the 
        /// messages it receives. The server will assume the client has received the message as soon as it 
        /// sends it to the client. This acknowledgment mode can cause messages being transmitted to the client 
        /// to get dropped.
        /// </summary>
        Auto,
        /// <summary>
        /// When the ack mode is client, then the client MUST send the server ACK frames 
        /// for the messages it processes. If the connection fails before a client sends 
        /// an ACK frame for the message the server will assume the message has not been 
        /// processed and MAY redeliver the message to another client. The ACK frames sent 
        /// by the client will be treated as a cumulative acknowledgment. This means the 
        /// acknowledgment operates on the message specified in the ACK frame and all 
        /// messages sent to the subscription before the ACK'ed message.
        /// </summary>
        Client,
        /// <summary>
        /// When the ack mode is client-individual, the acknowledgment operates just like 
        /// the client acknowledgment mode except that the ACK or NACK frames sent by the 
        /// client are not cumulative. This means that an ACK or NACK frame for a subsequent 
        /// message MUST NOT cause a previous message to get acknowledged.
        /// </summary>
        ClientIndividual
    }
}
