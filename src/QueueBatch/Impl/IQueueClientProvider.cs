using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl
{
    interface IQueueClientProvider
    {
        CloudQueueClient GetClient();
    }

    class QueueClientProvider : IQueueClientProvider
    {
        readonly CloudQueueClient queues;

        public QueueClientProvider(IConfiguration config)
        {
            var connectionString = config.GetWebJobsConnectionString(ConnectionStringNames.Storage);
            queues = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();
        }

        public CloudQueueClient GetClient() => queues;
    }
}