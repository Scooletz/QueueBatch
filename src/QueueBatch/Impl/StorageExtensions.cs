using System;
using Microsoft.WindowsAzure.Storage;

namespace QueueBatch.Impl
{
    static class StorageExceptionExtensions
    {
        /// <summary>
        ///     Determines whether the exception is due to a 400 Bad Request error with the error code PopReceiptMismatch.
        /// </summary>
        /// <param name="exception">The storage exception.</param>
        /// <returns>
        ///     <see langword="true" /> if the exception is due to a 400 Bad Request error with the error code
        ///     PopReceiptMismatch; otherwise <see langword="false" />.
        /// </returns>
        public static bool IsBadRequestPopReceiptMismatch(this StorageException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var result = exception.RequestInformation;

            if (result == null)
                return false;

            if (result.HttpStatusCode != 400)
                return false;

            var extendedInformation = result.ExtendedErrorInformation;

            if (extendedInformation == null)
                return false;

            return extendedInformation.ErrorCode == "PopReceiptMismatch";
        }
        
        /// <summary>
        ///     Determines whether the exception is due to a 409 Conflict error with the error code QueueBeingDeleted or
        ///     QueueDisabled.
        /// </summary>
        /// <param name="exception">The storage exception.</param>
        /// <returns>
        ///     <see langword="true" /> if the exception is due to a 409 Conflict error with the error code QueueBeingDeleted
        ///     or QueueDisabled; otherwise <see langword="false" />.
        /// </returns>
        public static bool IsConflictQueueBeingDeletedOrDisabled(this StorageException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var result = exception.RequestInformation;

            if (result == null)
                return false;

            if (result.HttpStatusCode != 409)
                return false;

            var extendedInformation = result.ExtendedErrorInformation;

            if (extendedInformation == null)
                return false;

            return extendedInformation.ErrorCode == "QueueBeingDeleted";
        }

        /// <summary>
        ///     Determines whether the exception is due to a 404 Not Found error with the error code MessageNotFound or
        ///     QueueNotFound.
        /// </summary>
        /// <param name="exception">The storage exception.</param>
        /// <returns>
        ///     <see langword="true" /> if the exception is due to a 404 Not Found error with the error code MessageNotFound
        ///     or QueueNotFound; otherwise <see langword="false" />.
        /// </returns>
        public static bool IsNotFoundMessageOrQueueNotFound(this StorageException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var result = exception.RequestInformation;

            if (result == null)
                return false;

            if (result.HttpStatusCode != 404)
                return false;

            var extendedInformation = result.ExtendedErrorInformation;

            if (extendedInformation == null)
                return false;

            var errorCode = extendedInformation.ErrorCode;
            return errorCode == "MessageNotFound" || errorCode == "QueueNotFound";
        }

        /// <summary>
        ///     Determines whether the exception is due to a 404 Not Found error with the error code QueueNotFound.
        /// </summary>
        /// <param name="exception">The storage exception.</param>
        /// <returns>
        ///     <see langword="true" /> if the exception is due to a 404 Not Found error with the error code QueueNotFound;
        ///     otherwise <see langword="false" />.
        /// </returns>
        public static bool IsNotFoundQueueNotFound(this StorageException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var result = exception.RequestInformation;

            if (result == null)
                return false;

            if (result.HttpStatusCode != 404)
                return false;

            var extendedInformation = result.ExtendedErrorInformation;

            if (extendedInformation == null)
                return false;

            return extendedInformation.ErrorCode == "QueueNotFound";
        }

        public static bool IsServerSideError(this StorageException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            var requestInformation = exception.RequestInformation;
            if (requestInformation == null)
                return false;
            int httpStatusCode = requestInformation.HttpStatusCode;
            return httpStatusCode >= 500 && httpStatusCode < 600;
        }
    }
}