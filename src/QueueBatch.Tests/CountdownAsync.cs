using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBatch.Tests
{
    class CountdownAsync
    {
        int value;
        readonly TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        static readonly object Result = new object();

        public CountdownAsync(int value)
        {
            this.value = value;
        }

        public void Release(int count = 1)
        {
            var result = Interlocked.Add(ref value, -count);
            if (result == 0)
            {
                tcs.TrySetResult(Result);
            }
            else if (result < 0)
            {
                tcs.TrySetException(new Exception("Countdown went below zero"));
            }
        }

        public Task Wait() => tcs.Task;
    }
}