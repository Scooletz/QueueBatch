using System.Reflection;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl
{
    static class CloudQueueMessageExtensions
    {
        static readonly PropertyInfo Id;
        static readonly PropertyInfo PopReceipt;

        static CloudQueueMessageExtensions()
        {
            Id = typeof(CloudQueueMessage).GetProperty(nameof(CloudQueueMessage.Id));
            PopReceipt = typeof(CloudQueueMessage).GetProperty(nameof(CloudQueueMessage.PopReceipt));
        }

        /// <summary>
        ///     Overwrites the message's Id and PopReceipt properties with the specified values, if different.
        /// </summary>
        public static void UpdateChangedProperties(this CloudQueueMessage message, string id, string popReceipt)
        {
            if (id != message.Id)
                Id.SetValue(message, id);

            if (popReceipt != message.PopReceipt)
                PopReceipt.SetValue(message, popReceipt);
        }
    }
}