using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Impl
{
    class MessageBatch : IMessageBatch
    {
        readonly Message[] messages;
        readonly HashSet<string> processed;

        static readonly HashSet<string> Empty = new HashSet<string>();

        public MessageBatch(Message[] messages)
        {
            this.messages = messages;
            processed = new HashSet<string>();
        }

        public IEnumerable<Message> Messages => messages;

        void IMessageBatch.MarkAsProcessed(Message message) => processed.Add(message.Id);

        void IMessageBatch.MarkAllAsProcessed()
        {
            foreach (var message in messages)
            {
                processed.Add(message.Id);
            }
        }

        public Task Complete(QueueFunctionLogic queue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            return Complete(queue, maxDequeueCount, visibilityTimeout, ct, processed);
        }

        public Task RetryAll(QueueFunctionLogic queue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            return Complete(queue, maxDequeueCount, visibilityTimeout, ct, Empty);
        }

        Task Complete(QueueFunctionLogic queue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct, HashSet<string> processed)
        {
            var tasks = new Task[messages.Length];
            for (var i = 0; i < messages.Length; i++)
            {
                var message = messages[i];

                if (processed.Contains(message.Id))
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