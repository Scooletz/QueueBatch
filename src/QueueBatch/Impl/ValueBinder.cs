using System.Reflection;
using System.Threading.Tasks;

namespace QueueBatch.Impl
{
    class ValueBinder : Microsoft.Azure.WebJobs.Extensions.Bindings.ValueBinder
    {
        readonly object value;

        public ValueBinder(ParameterInfo parameter, object value)
            : base(parameter.ParameterType)
        {
            this.value = value;
        }

        public override Task<object> GetValueAsync() => Task.FromResult(value);
        public override string ToInvokeString() => value.ToString();
    }
}