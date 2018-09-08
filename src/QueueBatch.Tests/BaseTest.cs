using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NUnit.Framework;
using QueueBatch.Impl;

namespace QueueBatch.Tests
{
    public abstract class BaseTest
    {
        public const string InputQueue = "inputbatch";
        public const string OutputQueue = "output";

        readonly CloudQueueClient queues;

        protected CloudQueue Batch { get; private set; }
        protected CloudQueue Output { get; private set; }
        protected CloudQueue Poison { get; private set; }

        const string ConnectionString = "UseDevelopmentStorage=true";

        protected BaseTest()
        {
            queues = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
        }

        [SetUp]
        public Task SetUp()
        {
            Batch = queues.GetQueueReference(InputQueue);
            Output = queues.GetQueueReference(OutputQueue);
            Poison = queues.GetQueueReference(InputQueue + BindingProvider.PoisonQueueSuffix);
            return Task.WhenAll(Batch.CreateIfNotExistsAsync(), Output.CreateIfNotExistsAsync(), Poison.CreateIfNotExistsAsync());
        }

        [TearDown]
        public Task TearDown() => Task.WhenAll(Batch.ClearAsync(), Output.ClearAsync(), Poison.CreateIfNotExistsAsync());

        protected async Task<List<string>> SendUnique(int count = 1)
        {
            var sends = new Task[count];
            var list = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var content = Guid.NewGuid().ToString("N");
                list.Add(content);
                sends[i] = Batch.AddMessageAsync(new CloudQueueMessage(content));
            }
            await Task.WhenAll(sends);
            return list;
        }

        protected static async Task RunHost<TFunctionProvidingType>(Func<Task> runner, TimeSpan? limit = null)
        {
            var limitValue = limit.GetValueOrDefault(TimeSpan.FromSeconds(15));
            using (var host = BuildHost<TFunctionProvidingType>())
            {
                await host.StartAsync();

                if (Debugger.IsAttached)
                {
                    await runner();
                }
                else
                {
                    await runner().LimitTo(limitValue);
                }

                await host.StopAsync();
            }
        }

        static JobHost BuildHost<TFunctionProvidingType>()
        {
            var config = new JobHostConfiguration
            {
                HostId = Guid.NewGuid().ToString("n"),
                TypeLocator = new SingleTypeLocator<TFunctionProvidingType>(),
                StorageConnectionString = ConnectionString,
                DashboardConnectionString = ConnectionString,
            };

            config.UseQueueBatch();
            config.UseDevelopmentSettings();
            return new JobHost(config);
        }
    }
}