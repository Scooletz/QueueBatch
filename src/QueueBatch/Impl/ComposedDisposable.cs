using System;
using System.Collections.Generic;

namespace QueueBatch.Impl
{
    class ComposedDisposable : IDisposable
    {
        readonly IEnumerable<IDisposable> disposables;

        public ComposedDisposable(IEnumerable<IDisposable> disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}