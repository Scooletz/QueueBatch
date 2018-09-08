using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using QueueBatch.Impl;

namespace QueueBatch
{
    [Extension("QueueBatch", "Queues")]
    class QueueBatchExtensionConfigProvider : IExtensionConfigProvider
    {
        readonly BindingProvider provider;

        public QueueBatchExtensionConfigProvider(BindingProvider provider)
        {
            this.provider = provider;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<QueueBatchTriggerAttribute>()
                .BindToTrigger(provider);
        }
    }
}