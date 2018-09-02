using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch.Impl.Queues
{
    interface IQueue
    {
        Task<Result<IRetrievedMessages>> GetMessages(TimeSpan visibilityTimeout, CancellationToken ct);
        Task<Result<bool>> Put(Memory<byte> payload, CancellationToken ct);
        Task<Result<bool>> Update(Memory<byte> payload, string messageId, string popReceipt, TimeSpan visibilityTimeout, CancellationToken ct);
        Task<Result<bool>> Delete(string messageId, string popReceipt, CancellationToken ct);
    }
}