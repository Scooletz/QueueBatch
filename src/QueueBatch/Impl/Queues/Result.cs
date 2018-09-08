using System;
using System.Linq;
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
        {
            Code = response.StatusCode;
            ErrorCode = response.Headers.GetValues("x-ms-error-code").FirstOrDefault();
            ReasonPhrase = response.ReasonPhrase;
            Value = default;
        }

        public Result(HttpStatusCode code, string errorCode, string reasonPhrase)
        {
            Code = code;
            ErrorCode = errorCode;
            ReasonPhrase = reasonPhrase;
            Value = default;
        }

        public Result(HttpStatusCode code, T value)
        {
            Code = code;
            Value = value;
        }

        public readonly HttpStatusCode Code;
        public readonly string ErrorCode;
        public readonly T Value;
        public readonly string ReasonPhrase;

        public bool IsServerSideError
        {
            get
            {
                var status = (int) Code;
                return status >= 500 && status < 600;
            }
        }

        public Exception AsException() => new Exception(ErrorCode);
    }
}