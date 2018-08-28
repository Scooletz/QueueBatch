using System;

namespace QueueBatch
{
    /// <summary>
    /// A wrapper around processed message.
    /// </summary>
    public readonly struct Message
    {
        public Message(string id, Memory<byte> payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// The identifier of the message.
        /// </summary>
        public readonly string Id;
            
        /// <summary>
        /// The payload of the message.
        /// </summary>
        public readonly Memory<byte> Payload;
    }
}