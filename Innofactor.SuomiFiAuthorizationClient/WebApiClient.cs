using System;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Innofactor.SuomiFiAuthorizationClient {
    public class WebApiClient {

        private readonly HttpClient httpClient;

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
    }
}
