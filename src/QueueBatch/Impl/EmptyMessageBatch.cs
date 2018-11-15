using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch.Impl
{
    class EmptyMessageBatch : IMessageBatchImpl
    {
        public static readonly EmptyMessageBatch Instance = new EmptyMessageBatch();

        EmptyMessageBatch()
        { }

        static readonly IEnumerable<Message> Empty = new Message[0];

        public IEnumerable<Message> Messages => Empty;

        public void MarkAsProcessed(Message message) { }
        public void MarkAllAsProcessed() { }

        public Task Complete(CancellationToken ct) => Task.CompletedTask;
        public Task RetryAll(CancellationToken ct) => Task.CompletedTask;
    }
}