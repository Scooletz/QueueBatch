using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueueBatch.Impl
{
    interface ICloudStorageAccountProvider
    {
        CloudStorageAccount Get(string name);
    }

    class CloudStorageAccountProvider: ICloudStorageAccountProvider
    {
        readonly IConfiguration configuration;

        public CloudStorageAccountProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public CloudStorageAccount Get(string name)
        {            
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ConnectionStringNames.Storage;
            }

            var connectionString = configuration.GetWebJobsConnectionString(name);

            if (connectionString == null)
            {
                throw new InvalidOperationException($"Storage account '{name}' is not configured.");
            }

            return CloudStorageAccount.Parse(connectionString);
        }
    }
}
