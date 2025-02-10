﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsStaffReferral.Controllers
{
    public class SessionController : SessionControllerBase
    {
        private const string SESSION_KEY_STAFFREFERRAL_TOKEN = "StaffReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";
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
            HttpContext.Session.Remove(SESSION_KEY_STAFFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            //return
            return View();
        }
        [Route("{controller}/sign-out")]
        public IActionResult Signout()
        {
            //user wants to logout
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_STAFFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            //return
            return Redirect("/");
        }
    }
}
