using System;
using System.Collections.Generic;

namespace QueueBatch.Impl.Queues
{
    interface IRetrievedMessages : IDisposable
    {
        IEnumerable<Message> Messages { get; }
    }
}