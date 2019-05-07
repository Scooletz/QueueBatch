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
        /// Gets the maximum back-off timespan that can be used for the poller.
        /// </summary>
        public int MaxBackOffInSeconds { get; set; }

        /// <summary>
        /// The number of parallel GetMessagesAsync.
        /// </summary>
        public int ParallelGets { get; set; }

        /// <summary>
        /// If set to true, instead of using Azure Storage SDK Queues the listeners will use a custom implementation of queues client (with a tailored XML deserialization).
        /// </summary>
        public bool UseFasterQueues { get; set; }

        /// <summary>
        /// By default, the triggered function is executed only for non-empty batches. Setting this to true, will pass also the empty batches to execution.
        /// </summary>
        public bool RunWithEmptyBatch { get; set; }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string Connection { get; set; }

        /// <summary>
        /// If set to false, each message in same batch can be marked as processed independently and they will not affected when other messages cause exception, default to true
        /// </summary>
        public bool SuccessOrFailAsBatch { get; set; }
    }
}