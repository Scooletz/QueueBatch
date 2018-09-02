using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;

namespace QueueBatch.Impl.Queues
{
    class QueueFunctionLogic
    {
        readonly IQueue queue;
        readonly IQueue poisonQueue;

        public QueueFunctionLogic(IQueue queue, IQueue poisonQueue)
        {
            this.queue = queue;
            this.poisonQueue = poisonQueue;
        }

        public async Task<IRetrievedMessages> GetMessages(TimeSpan visibilityTimeout, CancellationToken ct)
        {
            var result = await queue.GetMessages(visibilityTimeout, ct).ConfigureAwait(false);
            if (result.Value != null)
            {
                return result.Value;
            }

            if (result.Code == HttpStatusCode.NotFound && result.ReasonString == QueueErrorCodeStrings.QueueNotFound)
            {
                return NoMessages.Instance;
            }

            if (result.Code == HttpStatusCode.Conflict && result.ReasonString == QueueErrorCodeStrings.QueueBeingDeleted)
            {
                return NoMessages.Instance;
            }

            if (result.IsServerSideError)
            {
                return NoMessages.Instance;
            }

            throw result.AsException();

        }

        public Task DeleteMessage(in Message message, CancellationToken ct) => DeleteImpl(message.Id, message.PopReceipt, ct);

        async Task DeleteImpl(string messageId, string popReceipt, CancellationToken ct)
        {
            var result = await queue.Delete(messageId, popReceipt, ct).ConfigureAwait(false);
            if (result.Value)
            {
                return;
            }

            if (result.Code == HttpStatusCode.BadRequest && result.ReasonString == QueueErrorCodeStrings.PopReceiptMismatch)
            {
                return;
            }

            if (result.Code == HttpStatusCode.NotFound)
            {
                if (result.ReasonString == QueueErrorCodeStrings.QueueNotFound || result.ReasonString == QueueErrorCodeStrings.MessageNotFound)
                {
                    return;
                }
            }

            if (result.Code == HttpStatusCode.Conflict && result.ReasonString == QueueErrorCodeStrings.QueueBeingDeleted)
            {
                return;
            }

            throw result.AsException();
        }


        public Task ReleaseMessage(in Message message, TimeSpan visibilityTimeout, CancellationToken ct) => ReleaseImpl(message.Payload, message.Id, message.PopReceipt, visibilityTimeout, ct);

        async Task ReleaseImpl(Memory<byte> payload, string messageId,
            string popReceipt, TimeSpan visibilityTimeout, CancellationToken ct)
        {
            var result = await queue.Update(payload, messageId, popReceipt, visibilityTimeout, ct).ConfigureAwait(false);
            if (result.Value)
            {
                return;
            }

            if (result.Code == HttpStatusCode.BadRequest && result.ReasonString == QueueErrorCodeStrings.PopReceiptMismatch)
            {
                return;
            }

            if (result.Code == HttpStatusCode.NotFound)
            {
                if (result.ReasonString == QueueErrorCodeStrings.QueueNotFound || result.ReasonString == QueueErrorCodeStrings.MessageNotFound)
                {
                    return;
                }
            }

            if (result.Code == HttpStatusCode.Conflict && result.ReasonString == QueueErrorCodeStrings.QueueBeingDeleted)
            {
                return;
            }

            throw result.AsException();
        }

        public Task MoveToPoisonQueue(in Message message, CancellationToken ct) => MoveImpl(message.Payload, message.Id, message.PopReceipt, ct);

        async Task MoveImpl(Memory<byte> payload, string id, string popReceipt, CancellationToken ct)
        {
            var putResult = await poisonQueue.Put(payload, ct);
            if (putResult.Value == false)
            {
                throw putResult.AsException();
            }

            await queue.Delete(id, popReceipt, ct).ConfigureAwait(false);
        }
    }
}