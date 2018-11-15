using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Impl
{
    class Listener : IListener
    {
        readonly RandomizedExponentialBackoffStrategy backOff;
        readonly ITriggeredFunctionExecutor executor;
        readonly int maxRetries;
        readonly QueueFunctionLogic queue;
        readonly TimeSpan visibilityTimeout;
        readonly bool shouldRunOnEmptyBatch;
        readonly ILoggerFactory loggerFactory;
        readonly Task<IRetrievedMessages>[] gets;

        Task runner;
        CancellationTokenSource tokenSource;
        static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(10.0);

        public Listener(ITriggeredFunctionExecutor executor, QueueFunctionLogic queue,
            TimeSpan maxBackOff, int maxRetries, TimeSpan visibilityTimeout, int parallelGets,
            bool shouldRunOnEmptyBatch, ILoggerFactory loggerFactory)
        {
            this.executor = executor;
            this.queue = queue;
            this.maxRetries = maxRetries;
            this.visibilityTimeout = visibilityTimeout;
            this.shouldRunOnEmptyBatch = shouldRunOnEmptyBatch;
            this.loggerFactory = loggerFactory;
            gets = new Task<IRetrievedMessages>[parallelGets];
            backOff = new RandomizedExponentialBackoffStrategy(TimeSpan.FromMilliseconds(100), maxBackOff);
        }

        public Task StartAsync(CancellationToken ct)
        {
            tokenSource = new CancellationTokenSource();
            runner = Task.Run(Process, ct);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            Cancel();
            return runner;
        }

        public void Dispose()
        {
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public void Cancel()
        {
            tokenSource.Cancel();
        }

        async Task Process()
        {
            var logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("BatchedQueue"));

            var ct = tokenSource.Token;
            while (tokenSource.IsCancellationRequested == false)
            {
                try
                {
                    for (var i = 0; i < gets.Length; i++)
                    {
                        gets[i] = queue.GetMessages(VisibilityTimeout, ct);
                    }

                    var results = await Task.WhenAll(gets).ConfigureAwait(false);

                    using (new ComposedDisposable(results))
                    {
                        var messages = results.SelectMany(msgs => msgs.Messages).ToArray();
                        var isNotEmpty = messages.Length > 0;

                        if (isNotEmpty || shouldRunOnEmptyBatch)
                        {
                            var batch = isNotEmpty
                                ? (IMessageBatchImpl) new MessageBatch(messages, queue, maxRetries, visibilityTimeout)
                                : EmptyMessageBatch.Instance;

                            var data = new TriggeredFunctionData {TriggerValue = batch};
                            var result = await executor.TryExecuteAsync(data, CancellationToken.None).ConfigureAwait(false);

                            try
                            {
                                if (result.Succeeded)
                                {
                                    await batch.Complete(CancellationToken.None);
                                }
                                else
                                {
                                    await batch.RetryAll(CancellationToken.None);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError("Exception occured when completing batch", ex);
                                await Delay(false, ct).ConfigureAwait(false);
                            }

                            // on empty, back-off is performed as usual
                            await Delay(result.Succeeded && isNotEmpty, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            logger.LogDebug("No messages received");
                            await Delay(false, ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex) when (ex.InnerException != null &&
                                           ex.InnerException.GetType() == typeof(TaskCanceledException))
                {
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        Task Delay(bool executionSucceeded, CancellationToken ct) => Task.Delay(backOff.GetNextDelay(executionSucceeded), ct);
    }
}