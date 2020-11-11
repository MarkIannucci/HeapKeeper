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
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;

        public AzureDevOpsTalkerService(HttpClient httpClient, AzureDevOpsOAuthOptions devOpsOAuthOptions, IHttpContextAccessor httpContextAccessor, ILogger<AzureDevOpsTalkerService> logger)
        {
            _httpClient = httpClient;
            _devOpsOAuthOptions = devOpsOAuthOptions;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
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
            // https://stackoverflow.com/a/53577368/1944366
            string myHostUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";


            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _devOpsOAuthOptions.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            requestMessage.Content = new FormUrlEncodedContent( new Dictionary<String, String>()
                {
                    { "client_assertion_type", AzureDevOpsAuthenticationDefaults.ClientAssertionType },
                    { "client_assertion", _devOpsOAuthOptions.ClientSecret },
                    { "grant_type", "refresh_token" },
                    { "assertion", refreshToken },
                    { "redirect_uri", myHostUrl + _devOpsOAuthOptions.CallbackPath }
                });
            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                // Handle successful request
                String body = await responseMessage.Content.ReadAsStringAsync();
                return JObject.Parse(body).ToObject<AzDoToken>();
            } else
            {
                
                _logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                responseMessage.StatusCode,
                                responseMessage.Headers.ToString(),
                                await responseMessage.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while refreshing the token.");
            }
        }
    }
}
