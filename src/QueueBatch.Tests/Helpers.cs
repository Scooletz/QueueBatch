using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using NUnit.Framework;

namespace QueueBatch.Tests
{
    static class Helpers
    {
        public static async Task LimitTo(this Task t, TimeSpan maxRunTime)
        {
            var cts = new CancellationTokenSource();

            if (await Task.WhenAny(t, Task.Delay(maxRunTime, cts.Token)) == t)
            {
                cts.Cancel();
                await t.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException("Task not finished in time.");
            }
        }

        public static async Task<List<CloudQueueMessage>> Drain(this CloudQueue queue, int number)
        {
            var received = new List<CloudQueueMessage>();
            while (received.Count != number)
            {
                var messages = await queue.GetMessagesAsync(32);
                foreach (var message in messages)
                {
                    received.Add(message);
                    await queue.DeleteMessageAsync(message);
                }
            }

            return received;
        }
        public static async Task AssertIsEmpty(this CloudQueue queue)
        {
            var messages = await queue.GetMessagesAsync(32);
            CollectionAssert.IsEmpty(messages);
        }
    }
}
