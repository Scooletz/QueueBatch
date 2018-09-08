using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl.Queues
{
    class SdkQueue : IQueue
    {
        readonly CloudQueue queue;

        public SdkQueue(CloudQueue queue)
        {
            this.queue = queue;
        }

        public async Task<Result<IRetrievedMessages>> GetMessages(TimeSpan visibilityTimeout, CancellationToken ct)
        {
            try
            {
                var cloudQueueMessages = await queue.GetMessagesAsync(CloudQueueMessage.MaxNumberOfMessagesToPeek, visibilityTimeout, null, null, ct).ConfigureAwait(false);
                var retrievedMessages = new RetrievedMessages(cloudQueueMessages.Select(m => new Message(m.Id, m.AsBytes, m.PopReceipt, m.DequeueCount)).ToArray());
                return new Result<IRetrievedMessages>(HttpStatusCode.OK, retrievedMessages);
            }
            catch (StorageException ex)
            {
                return FromException<IRetrievedMessages>(ex);
            }
        }

        public async Task<Result<bool>> Put(Memory<byte> payload, CancellationToken ct)
        {
            var message = new CloudQueueMessage(null);
            message.SetMessageContent(payload.ToArray());

            try
            {
                await queue.AddMessageAsync(message).ConfigureAwait(false);
                return new Result<bool>(HttpStatusCode.Created, true);
            }
            catch (StorageException ex)
            {
                return FromException<bool>(ex);
            }
        }

        public async Task<Result<bool>> Update(Memory<byte> payload, string messageId, string popReceipt, TimeSpan visibilityTimeout,
            CancellationToken ct)
        {
            var message = new CloudQueueMessage(messageId, popReceipt);
            message.SetMessageContent(payload.ToArray());

            try
            {
                await queue.UpdateMessageAsync(message, visibilityTimeout, MessageUpdateFields.Content | MessageUpdateFields.Visibility).ConfigureAwait(false);
                return new Result<bool>(HttpStatusCode.NoContent, true);
            }
            catch (StorageException ex)
            {
                return FromException<bool>(ex);
            }
        }

        public async Task<Result<bool>> Delete(string messageId, string popReceipt, CancellationToken ct)
        {
            try
            {
                await queue.DeleteMessageAsync(messageId, popReceipt).ConfigureAwait(false);
                return new Result<bool>(HttpStatusCode.NoContent, true);
            }
            catch (StorageException ex)
            {
                return FromException<bool>(ex);
            }
        }

        static Result<T> FromException<T>(StorageException ex) => new Result<T>((HttpStatusCode)ex.RequestInformation.HttpStatusCode, ex.RequestInformation.ExtendedErrorInformation.ErrorCode, ex.RequestInformation.ExtendedErrorInformation.ErrorMessage);

        class RetrievedMessages : IRetrievedMessages
        {
            public RetrievedMessages(Message[] messages)
            {
                Messages = messages;
            }

            public void Dispose() { }

            public IEnumerable<Message> Messages { get; }
        }

        public static FastCloudQueue CreateFast(CloudQueue queue, HttpMessageHandlerExpiringCache cache)
        {
            return new FastCloudQueue(queue.Uri, queue.GetSharedAccessSignature(new SharedAccessQueuePolicy
            {
                Permissions = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read | SharedAccessQueuePermissions.Update,
                SharedAccessExpiryTime = DateTimeOffset.Now.AddYears(1)
            }), cache);
        }
    }
}