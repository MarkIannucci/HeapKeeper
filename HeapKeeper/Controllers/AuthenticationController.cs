using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HeapKeeper.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpPost("~/signin")]
        public IActionResult SignIn([FromForm] string provider)
        {
            ChallengeResult challenge = Challenge(new AuthenticationProperties { 
                RedirectUri = "/"
            });         
            return challenge;
        }

        [HttpPost("~/signout")]
        public IActionResult SignOut()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
