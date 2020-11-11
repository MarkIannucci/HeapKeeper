using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Newtonsoft.Json;

namespace HeapKeeper.Models
{
    public class AzDoToken
    {
        [JsonProperty (PropertyName = "AccessToken")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "TokenType")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "RefreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "ExpiresIn")]
        public string ExpiresIn { get; set; }

        public AzDoToken(OAuthTokenResponse tokenResponse)
        {
            AccessToken = tokenResponse.AccessToken;
            TokenType = tokenResponse.TokenType;
            RefreshToken = tokenResponse.RefreshToken;
            ExpiresIn = tokenResponse.ExpiresIn;
        }
        public AzDoToken()
        {

        }
    }
}
