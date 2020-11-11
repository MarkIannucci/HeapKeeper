using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using HeapKeeper.BasicAuth;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;
using HeapKeeper.Models;

namespace HeapKeeper.Controllers
{
    public class CommentController : Controller
    {
        private readonly IAzureDevOpsTalkerService _talker;
        private readonly ICommentLinkerConfiguration _config;
        private readonly ICosmosDbService _cosmosDb;

        public CommentController(IAzureDevOpsTalkerService talker, ICommentLinkerConfiguration config
            , ICosmosDbService cosmosDb)
        {
            _talker = talker;
            _config = config;
            _cosmosDb = cosmosDb;
        }

        [HttpPost("~/comment")]
        [BasicAuth("CommentLinker")]
        public StatusCodeResult Post([FromBody] JsonElement jsonComment)
        {
            JObject comment = JObject.Parse(jsonComment.ToString());
            string htmlcomment = (string)comment["detailedMessage"]["html"];

            //trim off the first part of the message which tells us who commented to get us to the comment
            htmlcomment = htmlcomment.Substring(htmlcomment.IndexOf("<br/>") + 5);
            string ogHtmlComment = htmlcomment;

            //for each entry in the config injections, look for the regex and then replace using the evaluator 
            // https://stackoverflow.com/questions/306527/how-would-i-pass-additional-parameters-to-matchevaluator
            foreach (Injection injection in _config.Injections)
            {
                htmlcomment = Regex.Replace(htmlcomment, injection.RegexToFind, new MatchEvaluator(match => CreateLink(match, injection)));
            }

            // see if comment needs to be written back to DevOps.  If not stop here
            if (string.Equals(ogHtmlComment, htmlcomment))
            {
                return StatusCode(200);
            }

            // get useful data out of the stuff that AzDO sends us
            string commentId = (string)comment["resource"]["commentVersionRef"]["commentId"];
            string commentApiUrl = (string)comment["resource"]["commentVersionRef"]["url"];
            string organizationUrl = (string)comment["resourceContainers"]["project"]["baseUrl"];
            string project = (string)comment["resourceContainers"]["project"]["id"];
            string workItemId = (string)comment["resource"]["id"];
            string changedByDisplayName = (string)comment["resource"]["fields"]["System.ChangedBy"];

            // get the user from cosmos db
            string cosmosQuery = "SELECT * FROM CommentLinkUsers c WHERE c.displayName = \"" + changedByDisplayName + "\"";
            IEnumerable<CommentLinkUser> cmls = _cosmosDb.GetItemsAsync(cosmosQuery).Result;
            if (cmls.Count() > 1)
            {
                throw new Exception("There shouldn't be more than one user with the same display name");
            } else if (cmls.Count() == 0)
            {
                return StatusCode(200);
            }
            CommentLinkUser cml = cmls.First();

            // determine if we need to refresh the token
            if (DateTime.UtcNow > cml.TokenExpiration.AddSeconds(-300))
            {
                AzDoToken toke = _talker.RefreshToken(cml.Token.RefreshToken).Result;
                cml.Token = toke;
                _cosmosDb.UpsertItem(cml);
            }

            //now patch this back to AzureDevOps
            Dictionary<string, string> commentDictionary = new Dictionary<string, string>();
            commentDictionary.Add("text", htmlcomment);
            string json = JsonConvert.SerializeObject(commentDictionary, Formatting.Indented);
            HttpContent commentContent = new StringContent(json, Encoding.UTF8, "application/json");

            // start updating the comment
            string commentUri = organizationUrl + project + "/_apis/wit/workitems/" + workItemId + "/comments/" + commentId + "?api-version=6.0-preview.3";
            _ = _talker.Patch(commentUri, commentContent, cml.Token.AccessToken);
            return StatusCode(200);
        }

        public string CreateLink(Match match, Injection injection)
        {
            // identify # in the match text
            string textToInject = Regex.Match(match.Value, injection.RegexToInjectIntoLink).Value;

            // inject that into the link
            string link = injection.LinkToInject.Replace("`", textToInject);

            // compose a href html tag
            string htmllink = " <a href=\"" + link + "\">" + match.Value.Trim() + "</a> ";
            return htmllink;
        }
    }
}
