using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WmsMskReferral.Helpers;
using WmsMskReferral.Models;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;

namespace WmsMskReferral.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class MskHubController : SessionControllerBase
    {
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private readonly ILogger<MskHubController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWmsReferralService _WmsSelfReferralService;
        private readonly IODSLookupService _ODSLookupService;
        private TelemetryClient _telemetry;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MskHubController(ILogger<MskHubController> logger, IEmailSender emailSender, IWmsReferralService wmsReferralService,
            IODSLookupService odsLookupService, TelemetryClient telemetry, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _emailSender = emailSender;
            _WmsSelfReferralService = wmsReferralService;
            _ODSLookupService = odsLookupService;
            _telemetry = telemetry;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }



        [Route("{controller}/email-address")]
        public IActionResult Email()
        {
            var auth = GetAuthSessionData();
            return View(AddLoginClaims(auth));
        }
        [HttpPost]        
        [Route("{controller}/email-address")]
        public IActionResult Email(MskHubViewModel model)
        {
            //not needed here
            ModelState.Remove("ODSOrg");
            ModelState.Remove("MSKHubList");
            ModelState.Remove("SelectedMSKSite");

            if (!ModelState.IsValid) 
            { 
                return View(model);
            }

            var auth = GetAuthSessionData();

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAILADDRESS);
            if (!validDomain.IsMatch(model.EmailAddress))
            {
                ModelState.AddModelError("EmailAddress", "Email address not valid");
                return View(new MskHubViewModel { EmailAddress = model.EmailAddress });
            }

            //only update emailaddress            
            auth.EmailAddress = model.EmailAddress;
            SetAuthSessionData(auth);


            return RedirectToAction("select-msk-hub");
        }



        [Route("{controller}/")]
        [Route("{controller}/login")]
        [Route("{controller}/index")]
        [Route("{controller}/select-msk-hub")]        
        public async Task<IActionResult> MskHub()
        {
            var auth = GetAuthSessionData();
            //add login claims to session
            auth = AddLoginClaims(auth);
            
            try
            {
                var mskhubs = await _WmsSelfReferralService.GetMskHubs();
                if (mskhubs == null)
                {
                    _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", auth.EmailAddress }, { "Error", "MskHub API null" } });
                    //error, redirect
                    return View("GoneWrong", GetErrorModel("Service Error"));
                }

                auth.MskHubList = mskhubs;
            }
            catch (Exception ex)
            {

                _telemetry.TrackException(ex);
                return View("GoneWrong", GetErrorModel("Service Error"));
            }

            
            
            return View(auth);
        }

        [Route("{controller}/NHSMail-Landing")]
        public async Task<IActionResult> NHSLanding()
        {
            var auth = GetAuthSessionData();
            //add login claims to session
            auth = AddLoginClaims(auth);

            var ODSCodeCookie = GetODSCookie();
            if (ODSCodeCookie != "None")
            {
                //must be a valid ODS available via API
                try
                {

                    var mskhubs = await _WmsSelfReferralService.GetMskHubs();
                    if (mskhubs == null)
                    {
                        _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", auth.EmailAddress }, { "Error", "MskHub API null" } });
                        //error, redirect
                        return View("GoneWrong", GetErrorModel("Service Error"));
                    }

                    auth.MskHubList = mskhubs;
                }
                catch (Exception ex)
                {

                    _telemetry.TrackException(ex);
                    return View("GoneWrong", GetErrorModel("Service Error"));
                }

                if (auth.MskHubList.Where(w => w.OdsCode == ODSCodeCookie).Any())
                {
                    auth.ODSCode = ODSCodeCookie;
                    auth.SelectedMskHub = ODSCodeCookie;
                    var odsorg = await _ODSLookupService.LookupODSCodeAsync(auth.ODSCode);
                    if (odsorg != null)
                        auth.ODSOrg = odsorg;

                    SetAuthSessionData(auth);

                    return View("MskHubConfirm", auth);
                }

            }



           //else
            return RedirectToAction("select-msk-hub");
        }

        [Authorize]
        [HttpPost]       
        [Route("{controller}/select-msk-hub")]
        public async Task<IActionResult> MskHub(MskHubViewModel model)
        {
            ModelState.Remove("EmailAddress");
            ModelState.Remove("ODSOrg");
            ModelState.Remove("MSKHubList");
            var auth = GetAuthSessionData();

            if (!ModelState.IsValid)
            {
                try
                {
                    
                    var mskhubs = await _WmsSelfReferralService.GetMskHubs();
                    if (mskhubs == null)
                    {
                        _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", auth.EmailAddress }, { "Error", "MskHub API null" } });
                        //error, redirect
                        return View("GoneWrong", GetErrorModel("Service Error"));
                    }

                    model.MskHubList = mskhubs;
                }
                catch (Exception ex)
                {

                    _telemetry.TrackException(ex);
                    return View("GoneWrong", GetErrorModel("Service Error"));
                }
                return View(model);
            }


            
            auth.SelectedMskHub = model.SelectedMskHub;
            auth.ODSCode = model.SelectedMskHub;

            //set the users choice
            SetODSCookie(model.SelectedMskHub);


            var odsorg = await _ODSLookupService.LookupODSCodeAsync(auth.ODSCode);
            if (odsorg!=null)
                auth.ODSOrg = odsorg;

            SetAuthSessionData(auth);
            
            return View("MskHubConfirm", auth);
        }




        private string GetODSCookie()
        {
            var HubChoiceCookie = Request.Cookies[".WmsMskReferral.Hub"];
            if (HubChoiceCookie == null)
            {
                //no selection has been made
                return "None";
            }

            return HubChoiceCookie;
        }

        private void SetODSCookie(string ODSCode) 
        {

            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(".WmsMskReferral.Hub");
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(".WmsMskReferral.Hub", ODSCode, options);
                        
        }
        

        private MskHubViewModel AddLoginClaims(MskHubViewModel mskHubView)
        {
            var userClaims = ((ClaimsIdentity)User.Identity!).Claims;
            if (userClaims != null)
            {
                mskHubView.EmailAddress = userClaims.FirstOrDefault(w => w.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? "";
                mskHubView.NameIdentifier = userClaims.FirstOrDefault(w => w.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? "";
                mskHubView.ODSCode = userClaims.FirstOrDefault(w => w.Type == "ODS")?.Value ?? "";
                mskHubView.IsAuthorised = true;
            }
            SetAuthSessionData(mskHubView);

            return mskHubView;
        }


        private MskHubViewModel GetAuthSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<MskHubViewModel>(SESSION_KEY_EMAIL);
                if (srSession == null) //if its null midway may need to start again
                    return new MskHubViewModel { IsAuthorised = false };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new MskHubViewModel { };
        }
        private ErrorViewModel GetErrorModel(string message, string traceid = "")
        {
            _telemetry.TrackTrace(message);
            return new ErrorViewModel()
            {
                RequestId = "",
                TraceId = traceid,
                Message = $"Error: {message}"
            };
        }
        private void SetAuthSessionData(MskHubViewModel auth)
        {
            try
            {
                HttpContext.Session.Set<MskHubViewModel>(SESSION_KEY_EMAIL, auth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
