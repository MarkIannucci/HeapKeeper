using System;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeapKeeper
{
    using HeapKeeper.Models;
    using Newtonsoft.Json;
    using System.Data;

    public class CommentLinkUser
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        
        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }
        
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "token")]
        public AzDoToken Token { get; private set; }
        
        [JsonProperty(PropertyName = "tokenExpiration")]
        public DateTime TokenExpiration { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        public CommentLinkUser(string nameIdentifier, string emailAddress, string name, OAuthTokenResponse token)
        {
            Id = nameIdentifier;
            EmailAddress = emailAddress;
            Name = name;
            Token = new AzDoToken(token);
            TokenExpiration = DateTime.UtcNow.AddSeconds(Convert.ToDouble(token.ExpiresIn));
            DisplayName = name + " <" + emailAddress + ">";
        }
        public void UpdateToken(AzDoToken toke)
        {
            Token = toke;
            TokenExpiration = DateTime.UtcNow.AddSeconds(Convert.ToDouble(toke.ExpiresIn));
        }
        
        public CommentLinkUser()
        {

        }
    }
}
