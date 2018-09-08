using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace QueueBatch.Impl
{
    class ValueProvider : IValueProvider
    {
        readonly object value;

        public ValueProvider(ParameterInfo parameter, object value)
        {
            this.value = value;
            Type = parameter.ParameterType;
        }

        public Task<object> GetValueAsync() => Task.FromResult(value);
        public string ToInvokeString() => value.ToString();
        public Type Type { get; }
    }
}