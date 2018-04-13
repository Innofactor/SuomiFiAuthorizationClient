using Innofactor.SuomiFiAuthorizationClient.Config;
using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Innofactor.SuomiFiAuthorizationClient {

    public class WebApiClient {

        private readonly HttpClient httpClient;
        private readonly SuomiFiAuthorizationConfig authConfig;

        public WebApiClient(HttpClient httpClient, SuomiFiAuthorizationConfig authConfig) {
            this.httpClient = httpClient;
            this.authConfig = authConfig;
        }

        private async Task<T> GetObject<T>(HttpRequestMessage request) where T : class {

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccess();

            var result = await ReadObject<T>(response);
            return result;

        }

        private async Task<T> ReadObject<T>(HttpResponseMessage response) where T : class {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = await response.Content.ReadAsStreamAsync();
            return serializer.ReadObject(stream) as T;
        }

        private async Task<OAuthResponse> ChangeCodeToToken(string code, string callbackUri) {

            var auth = AuthorizationHeaderFactory.CreateBasicHeader(authConfig.ClientId, authConfig.OAuthSecret);
            var absUrl = authConfig.RovaHost + "/oauth/token?code=" + code + "&grant_type=authorization_code&redirect_uri=" + callbackUri;
            var request = new HttpRequestMessage(HttpMethod.Post, absUrl) {
                Headers = {
                  { "Authorization", auth }
                }
            };

            var result = await GetObject<OAuthResponse>(request);
            return result;

        }

    }

    [DataContract]
    public class OAuthResponse {
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }
    }

}
