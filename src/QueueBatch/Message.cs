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
            PopReceipt = null;
            DequeueCount = 0;
        }

        internal Message(string id, Memory<byte> payload, string popReceipt, int dequeueCount)
        {
            Id = id;
            Payload = payload;
            PopReceipt = popReceipt;
            DequeueCount = dequeueCount;
        }

        /// <summary>
        /// The identifier of the message.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public readonly Memory<byte> Payload;

        /// <summary>
        /// The pop receipt of this message.
        /// </summary>
        internal readonly string PopReceipt;

        /// <summary>
        /// The dequeue count.
        /// </summary>
        internal readonly int DequeueCount;
    }
}