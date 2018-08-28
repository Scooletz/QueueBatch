using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Description;

namespace QueueBatch
{
    /// <summary>
    /// Attribute used to bind a parameter to a batch of Azure Queue messages, causing the function to run when messages are enqueued.
    /// </summary>
    /// <remarks>
    /// The method parameter must be of type <see cref="IMessageBatch" />
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    [DebuggerDisplay("{" + nameof(QueueName) + ",nq}")]
    [Binding]
    public class QueueBatchTriggerAttribute : Attribute
    {
        /// <inheritdoc />
        /// <summary>Initializes a new instance of the <see cref="T:Microsoft.Azure.WebJobs.QueueBatchTriggerAttribute" /> class.</summary>
        /// <param name="queueName">The name of the queue to which to bind.</param>
        public QueueBatchTriggerAttribute(string queueName)
        {
            QueueName = queueName;
            MaxBackOffInSeconds = 60;
            ParallelGets = 1;
        }

        /// <summary>Gets the name of the queue to which to bind.</summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets the maximum backoff timespan that can be used for the poller.
        /// </summary>
        public int MaxBackOffInSeconds { get; set; }

        /// <summary>
        /// The number of parallel GetMessagesAsync.
        /// </summary>
        public int ParallelGets { get; set; }
    }
}