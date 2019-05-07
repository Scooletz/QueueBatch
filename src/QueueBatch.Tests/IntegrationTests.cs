using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using NUnit.Framework;

namespace QueueBatch.Tests
{
    public class IntegrationTests : BaseTest
    {
        [Test]
        public async Task Messages_failed_as_batch_when_SuccessOrFailAsBatch_set_true()
        {
            const int count = 4;
            await SendUnique(count);
            await Batch.AddMessageAsync(new CloudQueueMessage("bad-guid"));

            await RunHost<SuccessOrFailAsBatchTrue>(async () =>
            {
                await Poison.Drain(5);
                await Batch.AssertIsEmpty();
            });
        }

        [Test]
        public async Task Can_mark_message_as_processed_while_other_cause_exception_when_SuccessOrFailAsBatch_set_false()
        {
            const int count = 4;
            await SendUnique(count);
            await Batch.AddMessageAsync(new CloudQueueMessage("bad-guid"));

            await RunHost<SuccessOrFailAsBatchFalse>(async () =>
            {
                await Output.Drain(4);
                await Poison.Drain(1);
                await Batch.AssertIsEmpty();
            });
        }

        public class SuccessOrFailAsBatchTrue
        {
            public static async Task Do(
                  [QueueBatchTrigger(InputQueue, MaxBackOffInSeconds = 1, SuccessOrFailAsBatch = true)] IMessageBatch batch
                , [Queue(OutputQueue)] CloudQueue output)
            {
                await SomeMessageInBatchCauseException(batch, output);
            }
        }

        public class SuccessOrFailAsBatchFalse
        {
            public static async Task Do(
                  [QueueBatchTrigger(InputQueue, MaxBackOffInSeconds = 1, SuccessOrFailAsBatch = false)] IMessageBatch batch
                , [Queue(OutputQueue)] CloudQueue output)
            {
                await SomeMessageInBatchCauseException(batch, output);
            }            
        }

        private static async Task SomeMessageInBatchCauseException(IMessageBatch batch, CloudQueue output)
        {
            var messages = batch.Messages.ToList();

            foreach (var m in messages)
            {
                var content = Encoding.UTF8.GetString(m.Payload.Span);
                var guid = Guid.Parse(content);
                await output.AddMessageAsync(new CloudQueueMessage(m.Id));

                batch.MarkAsProcessed(m);
            }
        }

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
            public static async Task Do([QueueBatchTrigger(InputQueue, ParallelGets = 2, MaxBackOffInSeconds = 30)] IMessageBatch batch, [Queue(OutputQueue)] CloudQueue output)
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
                await Poison.Drain(count);
                await Batch.AssertIsEmpty();
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

        const int LotsCount = 1023;

        [Test]
        [Explicit("Benchmark like")]
        public async Task Batch_with_sdk()
        {
            await SendUnique(LotsCount);

            var sw = Stopwatch.StartNew();
            await RunHost<BatchWithSdk>(() => BatchWithSdk.CountDown.Wait(), TimeSpan.FromMinutes(5));
            Console.WriteLine($"Took: {sw.Elapsed}");
        }

        public class BatchWithSdk
        {
            internal static readonly CountdownAsync CountDown = new CountdownAsync(LotsCount);

            public static Task Do([QueueBatchTrigger(InputQueue, UseFasterQueues = false)] IMessageBatch batch)
            {
                CountDown.Release(batch.Messages.Count());
                batch.MarkAllAsProcessed();
                return Task.CompletedTask;
            }
        }

        [Test]
        [Explicit("Benchmark like")]
        public async Task Batch_with_custom()
        {
            await SendUnique(LotsCount);

            var sw = Stopwatch.StartNew();
            await RunHost<BatchWithCustom>(() => BatchWithCustom.CountDown.Wait(), TimeSpan.FromMinutes(5));
            Console.WriteLine($"Took: {sw.Elapsed}");
        }

        public class BatchWithCustom
        {
            internal static readonly CountdownAsync CountDown = new CountdownAsync(LotsCount);

            public static Task Do([QueueBatchTrigger(InputQueue, UseFasterQueues = true)] IMessageBatch batch)
            {
                CountDown.Release(batch.Messages.Count());
                batch.MarkAllAsProcessed();
                return Task.CompletedTask;
            }
        }

    }
}