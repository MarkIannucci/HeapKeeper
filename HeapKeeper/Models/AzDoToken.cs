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
    }
}
