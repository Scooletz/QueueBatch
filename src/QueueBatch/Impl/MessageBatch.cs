using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Impl
{
    class MessageBatch : IMessageBatchImpl
    {
        readonly Message[] messages;
        readonly QueueFunctionLogic queue;
        readonly int maxDequeueCount;
        readonly TimeSpan visibilityTimeout;
        readonly ConcurrentDictionary<string, string> processed;

        static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

        public MessageBatch(Message[] messages, QueueFunctionLogic queue, int maxDequeueCount, TimeSpan visibilityTimeout)
        {
            this.messages = messages;
            this.queue = queue;
            this.maxDequeueCount = maxDequeueCount;
            this.visibilityTimeout = visibilityTimeout;
            processed = new ConcurrentDictionary<string, string>();
        }

        public IEnumerable<Message> Messages => messages;

        void IMessageBatch.MarkAsProcessed(Message message) => processed.TryAdd(message.Id, message.Id);

        void IMessageBatch.MarkAllAsProcessed()
        {
            foreach (var message in messages)
            {
                processed.TryAdd(message.Id, message.Id);
            }
        }

        public Task Complete(CancellationToken ct) => Complete(ct, processed);

        public Task RetryAll(CancellationToken ct) => Complete(ct, Empty);

        Task Complete(CancellationToken ct, IReadOnlyDictionary<string, string> processed)
        {
            var tasks = new Task[messages.Length];

            for (var i = 0; i < messages.Length; i++)
            {
                var message = messages[i];

                if (processed.ContainsKey(message.Id))
                {
                    tasks[i] = queue.DeleteMessage(message, ct);
                }
                else
                {
                    if (message.DequeueCount >= maxDequeueCount)
                        tasks[i] = queue.MoveToPoisonQueue(message, ct);
                    else
                        tasks[i] = queue.ReleaseMessage(message, visibilityTimeout, ct);
                }
            }

            return Task.WhenAll(tasks);
        }

        public override string ToString() => $"{messages.Length} ASQ messages";
    }
}