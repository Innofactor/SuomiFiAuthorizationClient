using System;
using System.Net;
using System.Runtime.Serialization;

namespace Innofactor.SuomiFiAuthorizationClient {

    public class HttpStatusCodeException : Exception {

        public string Content { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public HttpStatusCodeException() {
        }

        /// <summary>
        /// Initializes HTTP status code exception.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="reason">Reason (may NOT contain line breaks).</param>
        /// <param name="content">Content (optional, may contain line breaks).</param>
        public HttpStatusCodeException(HttpStatusCode statusCode, string content, Exception innerException) :
          base("HTTP Status error " + statusCode, innerException) {
            this.StatusCode = statusCode;
            this.Content = content;
        }

        protected HttpStatusCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }

}
