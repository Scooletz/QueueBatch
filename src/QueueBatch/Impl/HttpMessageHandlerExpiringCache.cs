using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch.Impl
{
    /// <summary>
    /// A simple implementation for the lock-free cache of <see cref="HttpMessageHandler"/>
    /// </summary>
    class HttpMessageHandlerExpiringCache : IDisposable
    {
        readonly Timer timer;
        Entry entry;

        public HttpMessageHandlerExpiringCache(TimeSpan expire)
        {
            entry = new Entry();
            timer = new Timer(state => { RenewHandler(); }, null, expire, Timeout.InfiniteTimeSpan);
        }

        void RenewHandler()
        {
            // swap first to allow obtaining the new, then block the old one
            var oldEntry = Interlocked.Exchange(ref entry, new Entry());
            oldEntry.Block();
        }

        public HttpMessageHandler GetHandler()
        {
            // ReSharper disable once LocalVariableHidesMember
            var entry = Volatile.Read(ref this.entry);

            HttpMessageHandler handler;
            while (entry.TryGetHandler(out handler) == false)
            {
                entry = Volatile.Read(ref this.entry);
            }

            return new HttpTrackingHandler(handler, entry);
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        /// <summary>
        /// This class emulates <see cref="DelegatingHandler"/> without disposing the internal handler when the delegate is disposed.
        /// It's useful to have this behavior as it enables creating <see cref="HttpClient"/> without stopping it from disposing its message handler.
        /// With this approach, <see cref="HttpClient"/> disposes <see cref="HttpTrackingHandler"/> but this only releases the <see cref="entry"/> without disposing the underlying
        /// <see cref="innerHandler"/>.
        /// </summary>
        class HttpTrackingHandler : HttpMessageHandler
        {
            readonly HttpMessageHandler innerHandler;
            Entry entry;

            // ReSharper disable once StaticMemberInGenericType
            static readonly MethodInfo SendMethod;

            static HttpTrackingHandler()
            {
                SendMethod = typeof(HttpMessageHandler).GetMethod(nameof(SendAsync), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            public HttpTrackingHandler(HttpMessageHandler innerHandler, Entry entry)
            {
                this.innerHandler = innerHandler;
                this.entry = entry;
            }

            protected override void Dispose(bool disposing)
            {
                // ReSharper disable once LocalVariableHidesMember

                // ensures that release will occur only once
                var entry = Interlocked.Exchange(ref this.entry, null);
                if (entry == null)
                {
                    throw new ObjectDisposedException("The handler has already been disposed");
                }

                entry.Release();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return (Task<HttpResponseMessage>)SendMethod.Invoke(innerHandler, new object[] { request, cancellationToken });
            }
        }

        /// <summary>
        /// The entry provides a handler and a counter that keeps tracking of active leases of the handler.
        /// Once the entry is blocked with <see cref="Block"/>, the first thread that meets the <see cref="Blockage"/> value, will try to <see cref="SafeDispose"/> it.
        /// Once the <see cref="Block"/> is issued, no <see cref="TryGetHandler"/> will succeed.
        /// </summary>
        class Entry
        {
            const int Blockage = 1_000_000_000;
            int usages;

            HttpClientHandler handler = new HttpClientHandler();

            public void Release()
            {
                if (Interlocked.Decrement(ref usages) == Blockage)
                {
                    SafeDispose();
                }
            }

            void SafeDispose()
            {
                // ReSharper disable once LocalVariableHidesMember
                var handler = Interlocked.Exchange(ref this.handler, null);
                handler?.Dispose();
            }

            public void Block()
            {
                if (Interlocked.Add(ref usages, Blockage) == Blockage)
                {
                    SafeDispose();
                }
            }

            public bool TryGetHandler(out HttpMessageHandler value)
            {
                var lease = Interlocked.Increment(ref usages);
                if (lease < Blockage)
                {
                    value = handler;
                    return true;
                }

                // back off, it's blocked
                if (Interlocked.Decrement(ref usages) == Blockage)
                {
                    SafeDispose();
                }

                value = null;
                return false;
            }
        }
    }
}