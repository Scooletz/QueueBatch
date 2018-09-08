using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;
using QueueBatch.Impl;

namespace QueueBatch.Tests
{
    public class HttpMessageHandlerExpiringCacheTests
    {
        [Test]
        public async Task After_expiring_window_should_renew_handler()
        {
            var timeout = TimeSpan.FromMilliseconds(500);
            using (var cache = new HttpMessageHandlerExpiringCache(timeout))
            {
                var handler1 = GetHandler(cache, out var inner1);
                handler1.Dispose();

                await Task.Delay(timeout + timeout);

                var handler2 = GetHandler(cache, out var inner2);
                handler2.Dispose();

                Assert.AreNotEqual(inner1, inner2);
            }
        }

        [Test]
        public async Task Handler_held_after_renew_should_still_work()
        {
            var timeout = TimeSpan.FromMilliseconds(500);
            using (var cache = new HttpMessageHandlerExpiringCache(timeout))
            {
                var handler1 = GetHandler(cache, out var inner);
                var handler2 = GetHandler(cache, out _);

                await Task.Delay(timeout + timeout);

                handler1.Dispose();

                // inner should be still valid and this one request
                await DoSomeGet(inner, false);

                // handler 2 should free the inner handler
                handler2.Dispose();

                Assert.ThrowsAsync<ObjectDisposedException>(async () => await DoSomeGet(inner, false));
            }
        }

        [Test]
        public void Within_expiring_window_should_reuse_handler()
        {
            var timeout = TimeSpan.FromDays(1);
            using (var cache = new HttpMessageHandlerExpiringCache(timeout))
            {
                var handler1 = GetHandler(cache, out var inner1);
                handler1.Dispose();
                var handler2 = GetHandler(cache, out var inner2);
                handler2.Dispose();

                Assert.AreEqual(inner1, inner2);
            }
        }


        [Test]
        public void Disposing_twice_should_throw()
        {
            var timeout = TimeSpan.FromDays(1);
            using (var cache = new HttpMessageHandlerExpiringCache(timeout))
            {
                var handler = GetHandler(cache, out _);
                handler.Dispose();
                Assert.Throws<ObjectDisposedException>(() => handler.Dispose());
            }
        }

        static async Task DoSomeGet(HttpMessageHandler handler, bool disposeHandler = true)
        {
            using (var client = new HttpClient(handler, disposeHandler))
            {
                await client.GetAsync(CloudStorageAccount.DevelopmentStorageAccount.BlobStorageUri.PrimaryUri);
            }
        }

        static HttpMessageHandler GetHandler(HttpMessageHandlerExpiringCache cache, out HttpMessageHandler innerHandler)
        {
            var handler = cache.GetHandler();
            var fieldInfo = handler.GetType().GetField("innerHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            innerHandler = (HttpMessageHandler) fieldInfo.GetValue(handler);
            return handler;
        }
    }
}