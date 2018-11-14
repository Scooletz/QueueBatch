using System.Runtime.CompilerServices;
using Microsoft.Azure.WebJobs.Hosting;
using QueueBatch.Impl;

[assembly:InternalsVisibleTo("QueueBatch.Tests")]
[assembly:InternalsVisibleTo("QueueBatch.Benchmarks")]

[assembly: WebJobsStartup(typeof(QueueBatchStartup))]