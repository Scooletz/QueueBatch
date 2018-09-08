using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueueBatch.Impl;

namespace QueueBatch
{
    public static class QueueBatchExtensionConfigExtensions
    {
        public static void AddQueueBatch(this IWebJobsBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddExtension<QueueBatchExtensionConfigProvider>();
            builder.Services.TryAddSingleton<BindingProvider>();
            builder.Services.TryAddSingleton<IQueueClientProvider, QueueClientProvider>();
        }
    }
}