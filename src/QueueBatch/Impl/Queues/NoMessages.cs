using System.Collections.Generic;

namespace QueueBatch.Impl.Queues
{
    class NoMessages : IRetrievedMessages
    {
        public static readonly NoMessages Instance = new NoMessages();

        NoMessages() { }

        public void Dispose() { }

        public IEnumerable<Message> Messages { get; } = new Message[0];
    }
}