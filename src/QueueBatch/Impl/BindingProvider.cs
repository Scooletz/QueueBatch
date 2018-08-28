using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl
{
    class BindingProvider : ITriggerBindingProvider
    {
        public const string PoisonQueueSuffix = "-poison";

        readonly CloudQueueClient queues;
        readonly ILoggerFactory loggerFactory;

        public BindingProvider(JobHostConfiguration config)
        {
            queues = CloudStorageAccount.Parse(config.StorageConnectionString).CreateCloudQueueClient();
            loggerFactory = config.LoggerFactory;
        }

        public async Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attr = context.Parameter.GetCustomAttribute<QueueBatchTriggerAttribute>();

            if (attr == null)
                return null;

            var queueName = attr.QueueName;

            var queue = queues.GetQueueReference(queueName);
            var poisonQueue = CreatePoisonQueue(queueName);

            await Task.WhenAll(
                queue.CreateIfNotExistsAsync(),
                poisonQueue.CreateIfNotExistsAsync()
            ).ConfigureAwait(false);
            
            return new TriggerBinding(context.Parameter, queue, poisonQueue, TimeSpan.FromSeconds(attr.MaxBackOffInSeconds), attr.ParallelGets, loggerFactory);
        }

        CloudQueue CreatePoisonQueue(string name)
        {
            return queues.GetQueueReference(name + PoisonQueueSuffix);
        }
    }
}