using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace QueueBatch.Tests
{
    class SingleTypeLocator<T> : ITypeLocator
    {
        public IReadOnlyList<Type> GetTypes() => new[] { typeof(T) };
    }
}