using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Controllers
{
    public class SessionController : SessionControllerBase
    {
        private const string SESSION_KEY_SELFREFERRAL_TOKEN = "SelfReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";
        public IActionResult Index()
        {
            //return
            return Redirect("/");
        }

        [Route("{controller}/session-ended")]
        public async Task<IActionResult> EndSession()
        {
            //user wants to logout
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            //logout
            await HttpContext.SignOutAsync();

            //return
            return View();
        }
    }
}
