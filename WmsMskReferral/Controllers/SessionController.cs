using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsMskReferral.Controllers
{
    public class SessionController : SessionControllerBase
    {
        private const string SESSION_KEY_MSKREFERRAL_TOKEN = "MskReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        public IActionResult Index()
        {
            //return
            return Redirect("/");
        }

        [Route("{controller}/session-ended")]
        public IActionResult EndSession()
        {
            //user wants to logout
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_MSKREFERRAL_TOKEN);            
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);
                        
            //return
            return View();
        }
        [Route("{controller}/sign-out")]
        public IActionResult Signout()
        {
            //user wants to logout
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_MSKREFERRAL_TOKEN);            
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);

            //return
            return Redirect("/");
        }
    }
}
