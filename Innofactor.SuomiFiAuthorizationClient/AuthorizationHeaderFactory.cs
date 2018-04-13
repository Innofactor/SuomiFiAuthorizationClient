using System;
using System.Text;

namespace Innofactor.SuomiFiAuthorizationClient {

    public static class AuthorizationHeaderFactory {
        public static string Create(string clientId, string webApiKey, string path, DateTime timestamp) {

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            if (string.IsNullOrEmpty(webApiKey))
                throw new ArgumentNullException(nameof(webApiKey));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            // Adapted from https://github.com/vrk-kpa/roles-auths-web-api-node-client/blob/master/lib/HeaderUtils.js
            var timestampStr = timestamp.ToString("yyyy-MM-ddTHH\\:mm\\:ss.ffZ");
            var checksum = CryptUtils.Hash(path + " " + timestampStr, webApiKey);

            return clientId + " " + timestampStr + " " + checksum;

        }

        public static string CreateBasicHeader(string clientId, string clientSecret) {
            var content = clientId + ":" + clientSecret;
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
            return "Basic " + encoded;
        }

    }

}
