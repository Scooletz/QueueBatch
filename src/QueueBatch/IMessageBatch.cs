using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch
{
    /// <summary>
    /// Represents a batch of messages.
    /// </summary>
    public interface IMessageBatch
    {
        /// <summary>
        /// Gets the batch of messages.
        /// </summary>
        IEnumerable<Message> Messages { get; }

        /// <summary>
        /// Marks a specific message from the batch as processed. Processed messages won't be retried.
        /// </summary>
        /// <param name="message">A message obtained from <see cref="Messages"/>. </param>
        void MarkAsProcessed(Message message);

        /// <summary>
        /// Marks all the messages as processed.
        /// </summary>
        void MarkAllAsProcessed();
    }

    interface IMessageBatchImpl : IMessageBatch
    {
        Task Complete(CancellationToken ct);
        Task RetryAll(CancellationToken ct);
    }
}