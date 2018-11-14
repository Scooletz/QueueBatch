using System;
using Microsoft.Azure.WebJobs;

namespace QueueBatch
{
    /// <summary>
    /// Extension methods for QueueBatch
    /// </summary>
    public static class QueueBatchBuilderExtensions
    {
        /// <summary>
        /// Adds the QueueBatch extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        public static IWebJobsBuilder AddSignalR(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<QueueBatchExtensionConfigProvider>();

            return builder;
        }
    }
}