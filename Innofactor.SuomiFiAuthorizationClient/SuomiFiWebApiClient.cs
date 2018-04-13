using Innofactor.SuomiFiAuthorizationClient.Config;
using Innofactor.SuomiFiAuthorizationClient.Support;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Innofactor.SuomiFiAuthorizationClient {

    public class SuomiFiWebApiClient {

        private enum Mode {
            Hpa,
            Ypa
        }

        [DataContract]
        public class OAuthResponse {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
        }

        [DataContract]
        public class GetDelegateResponse {
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "personId")]
            public string PersonId { get; set; }
        }

        [DataContract]
        public class AuthorizationResult {
            [DataMember(Name = "result")]
            public string Result { get; set; }
        }

        [DataContract]
        public class OrganizationResult {
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "identifier")]
            public string Identifier { get; set; }
            [DataMember(Name = "roles")]
            public string[] Roles { get; set; }
        }

        private readonly IEncryptedCookieAccessor authStateAccessor;
        private readonly HttpClient httpClient;
        private readonly SuomiFiAuthorizationConfig authConfig;
        private readonly ILogger<SuomiFiWebApiClient> logger;
        private const string requestId = "suomiFiApiClient";
        private const string userId = "suomiFiUser";

        public SuomiFiWebApiClient(HttpClient httpClient, SuomiFiAuthorizationConfig authConfig, IEncryptedCookieAccessor authStateAccessor,
            ILoggerFactory loggerFactory) {
            this.httpClient = httpClient;
            this.authConfig = authConfig;
            this.authStateAccessor = authStateAccessor;
            this.logger = loggerFactory.CreateLogger<SuomiFiWebApiClient>();
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

        private async Task<SuomiFiAuthorizationResult> GetAuthorization(string sessionId, string accessToken, GetDelegateResponse principal) {

            var resourceUrl = $"/service/hpa/api/authorization/{sessionId}/{principal.PersonId}?requestId={requestId}&endUserId={userId}";
            var checksumHeaderValue = AuthorizationHeaderFactory.Create(authConfig.ClientId, authConfig.WebApiKey, resourceUrl, DateTime.UtcNow);
            var absUrl = authConfig.RovaHost + resourceUrl;

            var request = new HttpRequestMessage(HttpMethod.Get, absUrl) {
                Headers = {
                  { "Authorization", "Bearer " + accessToken },
                  { "X-AsiointivaltuudetAuthorization", checksumHeaderValue }
                }
            };

            var result = await GetObject<AuthorizationResult>(request);

            var fullResult = new SuomiFiAuthorizationResult {
                Name = principal.Name,
                PersonId = principal.PersonId,
                Allowed = result.Result == "ALLOWED"
            };

            return fullResult;

        }

        private async Task<SuomiFiAuthorizationResult[]> GetAuthorizations(string sessionId, string accessToken, GetDelegateResponse[] principals) {

            var tasks = principals.Select(p => GetAuthorization(sessionId, accessToken, p)).ToArray();
            var results = await Task.WhenAll(tasks);

            return results;

        }

        private async Task<GetDelegateResponse[]> GetDelegate(string sessionId, string accessToken) {

            var resourceUrl = $"/service/hpa/api/delegate/{sessionId}?requestId={requestId}&endUserId={userId}";
            var absUrl = authConfig.RovaHost + resourceUrl;
            var checksumHeaderValue = AuthorizationHeaderFactory.Create(authConfig.ClientId, authConfig.WebApiKey, resourceUrl, DateTime.UtcNow);
            var request = new HttpRequestMessage(HttpMethod.Get, absUrl) {
                Headers = {
                  { "Authorization", "Bearer " + accessToken },
                  { "X-AsiointivaltuudetAuthorization", checksumHeaderValue }
                }
            };

            var result = await GetObject<GetDelegateResponse[]>(request);
            return result;

        }

        private async Task<SuomiFiOrganizationResult[]> GetOrganizations(string sessionId, string accessToken) {
            var resourceUrl = $"/service/ypa/api/organizationRoles/{sessionId}?requestId={requestId}&endUserId={userId}";
            var absUrl = authConfig.RovaHost + resourceUrl;
            var checksumHeaderValue = AuthorizationHeaderFactory.Create(authConfig.ClientId, authConfig.WebApiKey, resourceUrl, DateTime.UtcNow);
            var request = new HttpRequestMessage(HttpMethod.Get, absUrl) {
                Headers = {
                  { "Authorization", "Bearer " + accessToken },
                  { "X-AsiointivaltuudetAuthorization", checksumHeaderValue }
                }
            };

            var result = await GetObject<OrganizationResult[]>(request);
            return result.Select(x => new SuomiFiOrganizationResult {
                Name = x.Name,
                BusinessId = x.Identifier,
                Roles = x.Roles
            }).ToArray();
        }

        private async Task UnregisterSession(Mode mode) {

            logger.LogDebug("Unregister session");

            var sessionId = authStateAccessor.Value;

            if (string.IsNullOrEmpty(sessionId))
                return;

            var registerPath = $"/service/{mode.ToString().ToLowerInvariant()}/user/unregister/{sessionId}";
            var auth = AuthorizationHeaderFactory.Create(authConfig.ClientId, authConfig.WebApiKey, registerPath, DateTime.UtcNow);

            var absUrl = authConfig.RovaHost + registerPath;
            var request = new HttpRequestMessage(HttpMethod.Get, absUrl) {
                Headers = {
                  { "X-AsiointivaltuudetAuthorization", auth }
                }
            };

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccess();

            logger.LogInformation("Session unregistered");

        }

    }

}
