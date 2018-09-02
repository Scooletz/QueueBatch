using System;
using System.Net;
using System.Net.Http;

namespace QueueBatch.Impl.Queues
{
    /// <summary>
    /// A result for operations that prefer to not throw an exception.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Result<T>
    {
        public Result(HttpResponseMessage response)
            : this(response.StatusCode, response.ReasonPhrase, default)
        { }

        public Result(HttpStatusCode code, string reasonString, T value)
        {
            Code = code;
            ReasonString = reasonString;
            Value = value;
        }

        public readonly HttpStatusCode Code;
        public readonly string ReasonString;
        public readonly T Value;

        public bool IsServerSideError
        {
            get
            {
                var status = (int) Code;
                return status >= 500 && status < 600;
            }
        }

        public Exception AsException() => new Exception(ReasonString);
    }
}