using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsStaffReferral.Models;


namespace WmsStaffReferral.Controllers
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
        [Route("/Error")]
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
