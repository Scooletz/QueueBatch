using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using QueueBatch.Impl.Queues;

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

            var messageQueue = queues.GetQueueReference(queueName);
            var poisonQueue = CreatePoisonQueue(queueName);

            await Task.WhenAll(
                messageQueue.CreateIfNotExistsAsync(),
                poisonQueue.CreateIfNotExistsAsync()
            ).ConfigureAwait(false);

            var cache = new HttpMessageHandlerExpiringCache(TimeSpan.FromSeconds(10));

            var queue = attr.UseFasterQueues
                ? new QueueFunctionLogic(SdkQueue.CreateFast(messageQueue, cache), SdkQueue.CreateFast(poisonQueue, cache))
                : new QueueFunctionLogic(new SdkQueue(messageQueue), new SdkQueue(poisonQueue));

            return new TriggerBinding(context.Parameter, queue, TimeSpan.FromSeconds(attr.MaxBackOffInSeconds), attr.ParallelGets, loggerFactory);
        }

        CloudQueue CreatePoisonQueue(string name)
        {
            return queues.GetQueueReference(name + PoisonQueueSuffix);
        }
    }
}