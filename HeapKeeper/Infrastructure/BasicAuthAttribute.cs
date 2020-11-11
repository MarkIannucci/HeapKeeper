using System;
using Microsoft.AspNetCore.Mvc;

namespace HeapKeeper.BasicAuth
{
    //https://codeburst.io/adding-basic-authentication-to-an-asp-net-core-web-api-project-5439c4cf78ee
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthAttribute : TypeFilterAttribute
    {
        public BasicAuthAttribute(string realm = @"CommentLinker") : base(typeof(BasicAuthFilter))
        {
            Arguments = new object[] { realm };
        }
    }
}