﻿using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using WmsMskReferral.Models;
using WmsReferral.Business.Models;

namespace WmsMskReferral.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private TelemetryClient _telemetry;

        public HomeController(ILogger<HomeController> logger, TelemetryClient telemetry)
        {
            _logger = logger;
            _telemetry = telemetry;
        }
        [Route("/")]
        [Route("/Home/Index")]
        public IActionResult Index()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "MskReferral");
            return View();
        }

        [Route("/Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
        [Route("/Accessibility")]
        public IActionResult Accessibility()
        {
            return View();
        }
        [Route("/Contact-Us")]
        public IActionResult ContactUs()
        {
            return View();
        }
        [Route("/Cookies")]
        public IActionResult Cookies()
        {
            return View();
        }
        [Route("/Terms-and-Conditions")]
        public IActionResult TermsAndConditions()
        {
            return View();
        }
        [Route("/access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [Route("/login-error")]
        [HttpPost]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GoneWrong()
        {
            var ptrace = HttpContext.Request.Query.ContainsKey("TraceId") == true ? HttpContext.Request.Query.Where(w => w.Key == "TraceId").First().Value.ToString() : null;
            var errorattrd = new Dictionary<string, string>()
            {
                { "TraceId", HttpContext.TraceIdentifier },
                { "RequestId", Activity.Current?.Id },
                { "UserIp1", HttpContext.Connection.RemoteIpAddress.ToString()},
                { "UserIp2", HttpContext.Request.Headers.TryGetValue("X-Azure-SocketIP", out StringValues UserIp) ? UserIp : StringValues.Empty },
                { "PriorTraceId", ptrace }
            };

            _telemetry.TrackEvent("GenericError", errorattrd);

            return View(new ErrorViewModel { Message = "We're having problems logging you in right now.", RequestId = Activity.Current.Id ?? HttpContext.TraceIdentifier, TraceId = ptrace ?? HttpContext.TraceIdentifier });
        }


        [Route("/Error")]
        [HttpPost]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {

            var errorattrd = new Dictionary<string, string>()
            {
                { "TraceId", HttpContext.TraceIdentifier },
                { "RequestId", Activity.Current?.Id },
                { "UserIp1", HttpContext.Connection.RemoteIpAddress.ToString()},
                { "UserIp2", HttpContext.Request.Headers.TryGetValue("X-Azure-SocketIP", out StringValues UserIp) ? UserIp : StringValues.Empty }
            };

            _telemetry.TrackEvent("GenericError", errorattrd);

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, TraceId = HttpContext.TraceIdentifier });
        }

        [Route("/ErrorHandler/{code:int}")]
        public IActionResult HandleError(int code)
        {
            var showmessage = code == 404;


            return View("~/Views/Shared/ErrorHandler.cshtml", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                TraceId = HttpContext.TraceIdentifier,
                StatusCode = code,
                Message = showmessage == true ? "Sorry, we can't find what you're looking for." : ""
            });
        }
    }
}