using System;
using System.Security.Cryptography;

namespace Innofactor.SuomiFiAuthorizationClient {

    public static class CryptUtils {
        public static string Hash(string message, string key) {

            var encoding = new System.Text.UTF8Encoding();
            var messageBytes = encoding.GetBytes(message);
            var keyBytes = encoding.GetBytes(key);

            using (var mac = new HMACSHA256(keyBytes)) {
                var hashed = mac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashed);
            }

        }
    }

}
