using System;
using Microsoft.Azure.WebJobs;

namespace QueueBatch
{
    public static class QueueBatchExtensionConfigExtensions
    {
        public static void UseQueueBatch(this JobHostConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.RegisterExtensionConfigProvider(new QueueBatchExtensionConfig());
        }
    }
}