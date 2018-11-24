using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Impl
{
    class BindingProvider : ITriggerBindingProvider
    {
        public const string PoisonQueueSuffix = "-poison";

        readonly ILoggerFactory loggerFactory;
        readonly ICloudStorageAccountProvider storageAccountProvider;

        public BindingProvider(ILoggerFactory loggerFactory, ICloudStorageAccountProvider storageAccountProvider)
        {
            this.loggerFactory = loggerFactory;
            this.storageAccountProvider = storageAccountProvider;
        }

        public async Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attr = context.Parameter.GetCustomAttribute<QueueBatchTriggerAttribute>();

            if (attr == null)
                return null;

            var queueName = attr.QueueName;
            var queueStorageConnection = attr.Connection;

            var storageAccount = storageAccountProvider.Get(queueStorageConnection);

            var queueClient = storageAccount.CreateCloudQueueClient();
            var messageQueue = queueClient.GetQueueReference(queueName);

            var poisonQueue = CreatePoisonQueue(queueClient, queueName);

            await Task.WhenAll(
                messageQueue.CreateIfNotExistsAsync(),
                poisonQueue.CreateIfNotExistsAsync()
            ).ConfigureAwait(false);

            var cache = new HttpMessageHandlerExpiringCache(TimeSpan.FromSeconds(10));

            var queue = attr.UseFasterQueues
                ? new QueueFunctionLogic(SdkQueue.CreateFast(messageQueue, cache), SdkQueue.CreateFast(poisonQueue, cache))
                : new QueueFunctionLogic(new SdkQueue(messageQueue), new SdkQueue(poisonQueue));

            return new TriggerBinding(context.Parameter, queue, TimeSpan.FromSeconds(attr.MaxBackOffInSeconds), attr.ParallelGets, attr.RunWithEmptyBatch, loggerFactory);
        }

        CloudQueue CreatePoisonQueue(CloudQueueClient queueClient, string name)
        {
            return queueClient.GetQueueReference(name + PoisonQueueSuffix);
        }
    }
}