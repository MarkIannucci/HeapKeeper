using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace HeapKeeper
{
    public class AzureDevOpsOAuthOptions : OAuthOptions
    {
        public AzureDevOpsOAuthOptions()
        {
            ClaimsIssuer = AzureDevOpsAuthenticationDefaults.Issuer;
            CallbackPath = AzureDevOpsAuthenticationDefaults.CallbackPath;
            AuthorizationEndpoint = AzureDevOpsAuthenticationDefaults.AuthorizationEndPoint;
            TokenEndpoint = AzureDevOpsAuthenticationDefaults.TokenEndPoint;
            UserInformationEndpoint = AzureDevOpsAuthenticationDefaults.UserInformationEndPoint;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "emailAddress");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        }
    }

    public static class AzureDevOpsAuthenticationDefaults
    {
        public const string AuthenticationScheme = "AzureDevOps";
        public const string Display = "AzureDevOps";
        public const string Issuer = "AzureDevOps";
        public const string CallbackPath = "/signin-azdo";
        public const string AuthorizationEndPoint = "https://app.vssps.visualstudio.com/oauth2/authorize";
        public const string TokenEndPoint = "https://app.vssps.visualstudio.com/oauth2/token";
        public const string UserInformationEndPoint = "https://app.vssps.visualstudio.com/_apis/profile/profiles/me";
        public const string ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    }

    public static class AzureDevOpsExtensions
    {
        public static AuthenticationBuilder AddAzureDevOps(this AuthenticationBuilder builder, Action<AzureDevOpsOAuthOptions> configuration) =>
            builder.AddOAuth<AzureDevOpsOAuthOptions, AzureDevOpsAuthenticationHandler>(
                AzureDevOpsAuthenticationDefaults.AuthenticationScheme,
                AzureDevOpsAuthenticationDefaults.Display,
                configuration);
    }

    public class AzureDevOpsAuthenticationHandler : OAuthHandler<AzureDevOpsOAuthOptions>
    {
        public ICosmosDbService CosmosDb { get; set; }
        public AzureDevOpsAuthenticationHandler(
            IOptionsMonitor<AzureDevOpsOAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ICosmosDbService cosmosDb)
            : base(options, logger, encoder, clock)
        {
            CosmosDb = cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb));
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            using var response = await Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                response.StatusCode,
                                response.Headers.ToString(),
                                await response.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var principal = new ClaimsPrincipal(identity);
            var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            context.RunClaimActions();

            await Options.Events.CreatingTicket(context);

            // store token and user in database
            string id = "";
            string emailAddress = "";
            string name = "";
            foreach(Claim claim in identity.Claims)
            {
                switch (claim.Type)
                {
                    case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":
                        id = claim.Value;
                        break;
                    case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
                        emailAddress = claim.Value;
                        break;
                    case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                        name = claim.Value;
                        break;
                }
            }
            CommentLinkUser commentLinkUser = new CommentLinkUser(id, emailAddress, name, tokens);
            _ = CosmosDb.UpsertItem(commentLinkUser);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["redirect_uri"] = context.RedirectUri,
                ["client_assertion"] = Options.ClientSecret,
                ["client_assertion_type"] = AzureDevOpsAuthenticationDefaults.ClientAssertionType,
                ["assertion"] = context.Code,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            });

            using var response = await Backchannel.SendAsync(request, Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving an access token: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                response.StatusCode,
                                response.Headers.ToString(),
                                await response.Content.ReadAsStringAsync());

                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var payload = JsonDocument.Parse(content);

            return OAuthTokenResponse.Success(payload);
        }

    }
}
