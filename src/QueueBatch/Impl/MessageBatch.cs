using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl
{
    class MessageBatch : IMessageBatch
    {
        readonly CloudQueueMessage[] messages;
        readonly HashSet<string> processed;

        static readonly HashSet<string> Empty = new HashSet<string>();

        public MessageBatch(CloudQueueMessage[] messages)
        {
            this.messages = messages;
            Messages = messages.Select(m => new Message(m.Id, new Memory<byte>(m.AsBytes))).ToArray();
            processed = new HashSet<string>();
        }

        public IEnumerable<Message> Messages { get; }

        void IMessageBatch.MarkAsProcessed(Message message) => processed.Add(message.Id);

        void IMessageBatch.MarkAllAsProcessed()
        {
            foreach (var message in messages)
            {
                processed.Add(message.Id);
            }
        }

        public Task Complete(CloudQueue queue, CloudQueue poisonQueue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            return Complete(queue, poisonQueue, maxDequeueCount, visibilityTimeout, ct, processed);
        }

        public Task RetryAll(CloudQueue queue, CloudQueue poisonQueue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            return Complete(queue, poisonQueue, maxDequeueCount, visibilityTimeout, ct, Empty);
        }

        Task Complete(CloudQueue queue, CloudQueue poisonQueue, int maxDequeueCount, TimeSpan visibilityTimeout,
            CancellationToken ct, HashSet<string> processed)
        {
            var tasks = new Task[messages.Length];
            for (var i = 0; i < tasks.Length; i++)
            {
                var message = messages[i];

                if (processed.Contains(message.Id))
                {
                    tasks[i] = DeleteMessageAsync(queue, message, ct);
                }
                else
                {
                    if (message.DequeueCount >= maxDequeueCount)
                        tasks[i] = MoveToPoisonQueue(queue, poisonQueue, message, ct);
                    else
                        tasks[i] = ReleaseMessageAsync(queue, message, visibilityTimeout, ct);
                }
            }

            return Task.WhenAll(tasks);
        }

        static async Task MoveToPoisonQueue(CloudQueue queue, CloudQueue poisonQueue, CloudQueueMessage message,
            CancellationToken ct)
        {
            var id = message.Id;
            var popReceipt = message.PopReceipt;
            await poisonQueue.AddMessageAsync(message, null, null, null, null, ct).ConfigureAwait(false);
            message.UpdateChangedProperties(id, popReceipt);
            await queue.DeleteMessageAsync(message.Id, message.PopReceipt, null, null, ct).ConfigureAwait(false);
        }

        static async Task DeleteMessageAsync(CloudQueue queue, CloudQueueMessage message, CancellationToken ct)
        {
            try
            {
                await queue.DeleteMessageAsync(message.Id, message.PopReceipt, null, null, ct).ConfigureAwait(false);

            }
            catch (StorageException ex)
            {
                if (ex.IsBadRequestPopReceiptMismatch() || ex.IsNotFoundMessageOrQueueNotFound() ||
                    ex.IsConflictQueueBeingDeletedOrDisabled())
                    return;

                throw;
            }
        }

        static async Task ReleaseMessageAsync(CloudQueue queue, CloudQueueMessage message, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            try
            {
                await queue.UpdateMessageAsync(message, visibilityTimeout, MessageUpdateFields.Visibility, null, null, ct);
            }
            catch (StorageException ex)
            {
                if (ex.IsBadRequestPopReceiptMismatch() || ex.IsNotFoundMessageOrQueueNotFound() ||
                    ex.IsConflictQueueBeingDeletedOrDisabled())
                    return;

                throw;
            }
        }

        public override string ToString() => $"{messages.Length} ASQ messages";
    }
}