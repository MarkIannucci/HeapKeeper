using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using HeapKeeper;
using HeapKeeper.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HeapKeeper
{
    public interface IAzureDevOpsTalkerService
    {
        HttpResponseMessage Patch(string uri, HttpContent content, string token);
        Task<AzDoToken> RefreshToken(string refreshToken);
    }

    public class AzureDevOpsTalkerService : IAzureDevOpsTalkerService
    {
        private HttpClient _httpClient;
        private AzureDevOpsOAuthOptions _devOpsOAuthOptions;

        public AzureDevOpsTalkerService(HttpClient httpClient, AzureDevOpsOAuthOptions devOpsOAuthOptions )
        {
            _httpClient = httpClient;
            _devOpsOAuthOptions = devOpsOAuthOptions;
        }

        public HttpResponseMessage Patch(string uri, HttpContent content, string token)
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage responseMessage = _httpClient.PatchAsync(uri, content).Result;
            return responseMessage;
        }

        public async Task<AzDoToken> RefreshToken(string refreshToken)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _devOpsOAuthOptions.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Dictionary<String, String> form = new Dictionary<String, String>()
                {
                    { "client_assertion_type", AzureDevOpsAuthenticationDefaults.ClientAssertionType },
                    { "client_assertion", _devOpsOAuthOptions.ClientSecret },
                    { "grant_type", "refresh_token" },
                    { "assertion", refreshToken },
                    { "redirect_uri", _devOpsOAuthOptions.CallbackPath }
                };
            requestMessage.Content = new FormUrlEncodedContent(form);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                // Handle successful request
                String body = await responseMessage.Content.ReadAsStringAsync();
                return JObject.Parse(body).ToObject<AzDoToken>();
            } else
            {
                throw new NotImplementedException("No error handling on failed token refresh");
            }
        }
    }
}
