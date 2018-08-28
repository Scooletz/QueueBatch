using Microsoft.Azure.WebJobs.Host.Config;
using QueueBatch.Impl;

namespace QueueBatch
{
    public class QueueBatchExtensionConfig : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<QueueBatchTriggerAttribute>()
                .BindToTrigger(new BindingProvider(context.Config));
        }
    }
}