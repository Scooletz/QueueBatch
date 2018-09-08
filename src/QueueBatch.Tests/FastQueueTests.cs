using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NUnit.Framework;
using QueueBatch.Impl;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Tests
{
    public class FastQueueTests
    {
        static readonly HttpMessageHandlerExpiringCache Cache = new HttpMessageHandlerExpiringCache(TimeSpan.FromMinutes(10));

        readonly List<CloudQueue> toClear = new List<CloudQueue>();
        SdkQueue sdkQueue;
        FastCloudQueue fastQueue;

        [Test]
        public async Task Delete_should_fail_on_mismatch()
        {
            Task<Result<bool>> Delete(IQueue queue) => queue.Delete("test", "test", CancellationToken.None);

            var sdk = await Delete(sdkQueue);
            var fast = await Delete(fastQueue);

            AssertEqual(sdk, fast);
        }

        [Test]
        public async Task Put_should_add()
        {
            Task<Result<bool>> Put(IQueue queue) => queue.Put(Memory<byte>.Empty, CancellationToken.None);

            var sdk = await Put(sdkQueue);
            var fast = await Put(fastQueue);

            AssertEqual(sdk, fast);
            await HasSameMessage();
        }

        [Test]
        public async Task Full_message_lifecycle()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5, 6, 7 };

            var _ = CancellationToken.None;
            await fastQueue.Put(payload, _);
            var messages = await fastQueue.GetMessages(TimeSpan.FromSeconds(10), _);
            using (messages.Value)
            {
                var one = messages.Value.Messages.Single();
                CollectionAssert.AreEqual(payload, one.Payload.ToArray(), "Payloads should be equal");
                await fastQueue.Delete(one.Id, one.PopReceipt, _);
            }

            CollectionAssert.IsEmpty((await fastQueue.GetMessages(TimeSpan.FromSeconds(10), _)).Value.Messages);

        }

        async Task HasSameMessage()
        {
            var messages = await Task.WhenAll(toClear.Select(q => q.GetMessagesAsync(32)));

            var payloads = new List<byte[]>();
            foreach (var q in messages)
            {
                payloads.Add(q.Single().AsBytes);
            }

            if (payloads.Count != 2)
            {
                throw new Exception("Tooooo many queues");
            }

            CollectionAssert.AreEqual(payloads[0], payloads[1]);
        }

        static void AssertEqual<T>(Result<T> a, Result<T> b, Comparison<T> comparison = null)
        {
            Assert.AreEqual(a.Code, a.Code);
            Assert.AreEqual(a.ErrorCode, a.ErrorCode);

            if (a.Value is IComparable<T> comparable)
            {
                Assert.AreEqual(0, comparable.CompareTo(b.Value));
            }
            else
            {
                if (comparison == null)
                {
                    throw new Exception("Provide comparison");
                }

                Assert.AreEqual(0, comparison(a.Value, b.Value));
            }
        }

        [SetUp]
        public async Task SetUp()
        {
            var queues = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();

            var test = TestContext.CurrentContext.Test;

            var queueName = test.MethodName.Replace("_", "").ToLower();
            var fast = queues.GetQueueReference(queueName + "1");
            var sdk = queues.GetQueueReference(queueName + "2");

            await Task.WhenAll(fast.CreateIfNotExistsAsync(), sdk.CreateIfNotExistsAsync());

            toClear.Add(fast);
            toClear.Add(sdk);

            sdkQueue = new SdkQueue(sdk);
            fastQueue = SdkQueue.CreateFast(fast, Cache);
        }

        [TearDown]
        public async Task TearDown()
        {
            await Task.WhenAll(toClear.Select(q => q.DeleteIfExistsAsync()));
            toClear.Clear();
        }
    }
}