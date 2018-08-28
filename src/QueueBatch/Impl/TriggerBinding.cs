using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueBatch.Impl
{
    class TriggerBinding : ITriggerBinding
    {
        readonly TimeSpan maxBackoff;
        readonly int parallelGets;
        readonly ILoggerFactory loggerFactory;
        readonly ParameterInfo param;
        readonly CloudQueue poisonQueue;
        readonly CloudQueue queue;

        public TriggerBinding(ParameterInfo param, CloudQueue queue, CloudQueue poisonQueue, TimeSpan maxBackoff, int parallelGets, ILoggerFactory loggerFactory)
        {
            this.param = param;
            this.queue = queue;
            this.poisonQueue = poisonQueue;
            this.maxBackoff = maxBackoff;
            this.parallelGets = parallelGets;
            this.loggerFactory = loggerFactory;
            BindingDataContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                {"data", typeof(IMessageBatch)}
            };
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var batch = (IMessageBatch) value;
            var data = new TriggerData(new ValueBinder(param, batch), new Dictionary<string, object>
            {
                {"data", batch}
            });

            return Task.FromResult<ITriggerData>(data);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(new Listener(context.Executor, queue, poisonQueue, maxBackoff, 5, TimeSpan.FromSeconds(1), parallelGets, loggerFactory));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                DisplayHints = new ParameterDisplayHints
                {
                    Description = "Name of the storage queue",
                    Prompt = "Please provide the storage queue name"
                },
                Name = param.Name
            };
        }

        public Type TriggerValueType => typeof(IMessageBatch);
        public IReadOnlyDictionary<string, Type> BindingDataContract { get; }
    }
}