using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch.Impl.Queues
{
    class FastCloudQueue : IQueue
    {
        const int GetAllocSize = 4 * 1024 * 1024;
        readonly ConcurrentQueue<byte[]> gettingPool = new ConcurrentQueue<byte[]>();

        const int PutAllocSize = 128 * 1024;
        readonly ConcurrentQueue<byte[]> puttingPool = new ConcurrentQueue<byte[]>();

        readonly HttpMessageHandlerExpiringCache handlerCache;
        readonly string messageUri;
        readonly string messageUriWithSas;
        readonly string sas;

        public FastCloudQueue(Uri queueUri, string sas, HttpMessageHandlerExpiringCache handlerCache)
        {
            this.handlerCache = handlerCache;
            messageUri = queueUri + "/messages";
            this.sas = GetSAS(sas);
            messageUriWithSas = queueUri + "/messages?" + this.sas;
        }

        public async Task<Result<IRetrievedMessages>> GetMessages(TimeSpan visibilityTimeout, CancellationToken ct)
        {
            var seconds = GetTimeout(visibilityTimeout);
            using (var http = GetClient())
            {
                var url = messageUriWithSas + "&numofmessages=32&visibilitytimeout=" + seconds;

                using (var response = await http.GetAsync(url, ct).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var bytes = GetBytes();
                        using (var stream = new MemoryStream(bytes))
                        {
                            await response.Content.CopyToAsync(stream).ConfigureAwait(false);
                            var messages = FastXmlAzureParser.ParseGetMessages(new Memory<byte>(bytes, 0, (int)stream.Length));

                            return new Result<IRetrievedMessages>(response.StatusCode, null, new RetrievedMessages(messages, bytes, this));
                        }
                    }

                    return new Result<IRetrievedMessages>(response);
                }
            }
        }

        public async Task<Result<bool>> Delete(string messageId, string popReceipt, CancellationToken ct)
        {
            using (var http = GetClient())
            {
                var url = messageUri + "/" + messageId + "?popreceipt=" + popReceipt + "&" + sas;
                using (var response = await http.DeleteAsync(url, ct).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return new Result<bool>(response.StatusCode, null, true);
                    }

                    return new Result<bool>(response);
                }
            }
        }

        public async Task<Result<bool>> Update(Memory<byte> payload, string messageId, string popReceipt, TimeSpan visibilityTimeout, CancellationToken ct)
        {
            var seconds = GetTimeout(visibilityTimeout);
            using (var http = GetClient())
            {
                var url = messageUri + "/" + messageId + "?popreceipt=" + popReceipt + "&visibilitytimeout=" + seconds +"&" + sas;
                using (var response = await http.PutAsync(url, new PutMessageContent(payload, this), ct).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return new Result<bool>(response.StatusCode, null, true);
                    }

                    return new Result<bool>(response);
                }
            }
        }

        public async Task<Result<bool>> Put(Memory<byte> payload, CancellationToken ct)
        {
            using (var http = GetClient())
            {
                using (var response = await http.PostAsync(messageUriWithSas, new PutMessageContent(payload, this), ct).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        return new Result<bool>(response.StatusCode, null, true);
                    }

                    return new Result<bool>(response);
                }
            }
        }

        static string GetTimeout(TimeSpan visibilityTimeout) => ((int)visibilityTimeout.TotalSeconds).ToString();

        HttpClient GetClient() => new HttpClient(handlerCache.GetHandler());
        byte[] GetBytes() => gettingPool.TryDequeue(out var bytes) ? bytes : new byte[GetAllocSize];
        void Return(byte[] bytes) => gettingPool.Enqueue(bytes);
        static string GetSAS(string sas) => sas.StartsWith("?") ? sas.Substring(1) : sas;

        class RetrievedMessages : IRetrievedMessages
        {
            readonly byte[] bytes;
            readonly FastCloudQueue queue;

            public RetrievedMessages(IEnumerable<Message> messages, byte[] bytes, FastCloudQueue queue)
            {
                this.bytes = bytes;
                this.queue = queue;
                Messages = messages;
            }

            public void Dispose()
            {
                queue.Return(bytes);
            }

            public IEnumerable<Message> Messages { get; }
        }

        class PutMessageContent : HttpContent
        {
            static readonly byte[] Prefix = Encoding.UTF8.GetBytes("<QueueMessage><MessageText>");
            static readonly byte[] Sufix = Encoding.UTF8.GetBytes("</MessageText></QueueMessage>");
            readonly FastCloudQueue queue;
            readonly Memory<byte> payload;

            public PutMessageContent(Memory<byte> payload, FastCloudQueue queue)
            {
                this.payload = payload;
                this.queue = queue;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                await stream.WriteAsync(Prefix, 0, Prefix.Length).ConfigureAwait(false);

                byte[] buffer = null;
                try
                {
                    buffer = queue.puttingPool.TryDequeue(out var bytes) ? bytes : new byte[PutAllocSize];

                    // encode and write payload
                    Base64.EncodeToUtf8(payload.Span, buffer, out _, out var written);
                    await stream.WriteAsync(buffer, 0, written).ConfigureAwait(false);
                }
                finally
                {
                    if (buffer != null)
                    {
                        queue.puttingPool.Enqueue(buffer);
                    }
                }

                await stream.WriteAsync(Sufix, 0, Sufix.Length).ConfigureAwait(false);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = Prefix.Length + Sufix.Length + Base64.GetMaxEncodedToUtf8Length(payload.Length);
                return true;
            }
        }
    }
}