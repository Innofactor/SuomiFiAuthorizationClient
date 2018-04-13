using System;
using System.Collections.Generic;
using System.Text;

namespace Innofactor.SuomiFiAuthorizationClient {
    public static class HttpResponseMessageHelpers {

        /// <summary>
        /// Ensures that HTTP response message contains success status code and throws <see cref="HttpStatusCodeException"/> if not.
        /// </summary>
        /// <param name="message">HTTP response message.</param>
        /// <remarks>Using this method ensures that the status code and reason phrase are captured to the exception.</remarks>
        public static void EnsureSuccess(this HttpResponseMessage message) {
            try {
                message.EnsureSuccessStatusCode();
            } catch (HttpRequestException x) {
                throw new HttpStatusCodeException(message.StatusCode, message.ReasonPhrase, x);
            }
        }

    }

}
