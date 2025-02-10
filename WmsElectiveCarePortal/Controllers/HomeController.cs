using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.ExternalConnectors;
using System.Diagnostics;
using WmsElectiveCarePortal.Models;
using WmsReferral.Business.Models;

namespace WmsElectiveCarePortal.Controllers
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

        public IActionResult Index()
        {

            var u = User.Identity;
            var claims = User.Claims;

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

        [Route("/redeem")]
        public IActionResult Redeem(string id_token)
        {
            //var redirectUrl = Url.Action(action:"index",controller: "ElectiveCare");
            //var properties = new AuthenticationProperties { RedirectUri = redirectUrl};
            //properties.Items["policy"] = "B2C_1A_INV_REDEEM";
            //properties.Items["tokenhint"] = id_token;
            //return Challenge(properties, "AB2C", "cookiesb2c");


            return RedirectToAction("index", "ElectiveCare");

        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    }
}