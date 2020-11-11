using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HeapKeeper.Models
{
    public class AzDoToken
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
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

        public AzDoToken(string json)
        {
            JObject jObj = JObject.Parse(json);
            AccessToken = (string)jObj["access_token"];
            TokenType = (string)jObj["token_type"];
            RefreshToken = (string)jObj["refresh_token"];
            ExpiresIn = (string)jObj["expires_in"];

        }
    }
}
