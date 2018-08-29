using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using NUnit.Framework;

namespace QueueBatch.Tests
{
    public class IntegrationTests : BaseTest
    {
        [Test]
        public async Task Simple_batch_dispatch()
        {
            const int count = 4;
            await SendUnique(count);

            await RunHost<SimpleBatchDispatch>(async () =>
            {
                await Output.Drain(count);
                await Batch.AssertIsEmpty();
            });
        }

        public class SimpleBatchDispatch
        {
            public static async Task Do([QueueBatchTrigger(InputQueue, ParallelGets = 2, MaxBackOffInSeconds = 30)] IMessageBatch batch, [Queue(OutputQueue)] CloudQueue output, TraceWriter trace)
            {
                foreach (var message in batch.Messages)
                {
                    await output.AddMessageAsync(new CloudQueueMessage(message.Id));
                    batch.MarkAsProcessed(message);
                }
            }
        }

        [Test]
        public async Task Errors_moved_to_poison()
        {
            const int count = 2;
            await SendUnique(count);

            await RunHost<ErrorsMovedToPoison>(async () =>
            {
                await Batch.AssertIsEmpty();
                await Poison.Drain(count);
            });
        }

        public class ErrorsMovedToPoison
        {
            public static Task Do([QueueBatchTrigger(InputQueue, ParallelGets = 2, MaxBackOffInSeconds = 1)] IMessageBatch batch)
            {
                // do nothing, do not ack any messages in batch
                return Task.CompletedTask;
            }
        }
    }
}