using GenericITSMSkill.Extensions;
using System;
using System.Net;
using System.Runtime.Serialization;

namespace GenericITSMSkill.Authorization.SAS
{
    /// <summary>
    /// The error response message exception.
    /// </summary>
    public class ErrorResponseMessageException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponseMessageException" /> class.
        /// </summary>
        /// <param name="httpStatus">The http status code.</param>
        /// <param name="errorMessage">The error response message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ErrorResponseMessageException(HttpStatusCode httpStatus, string errorMessage, Exception innerException = null)
            : base(errorMessage, innerException)
        {
            if (httpStatus.IsSuccessfulRequest())
            {
                throw new ArgumentException(
                    message: "The error response message exception should not be used for successful http response messages.",
                    paramName: nameof(httpStatus));
            }

            this.HttpStatus = httpStatus;
        }

        /// <summary>
        /// Gets the http status code.
        /// </summary>
        public HttpStatusCode HttpStatus { get; private set; }
    }
}