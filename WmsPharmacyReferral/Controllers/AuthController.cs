using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsPharmacyReferral.Helpers;
using WmsPharmacyReferral.Models;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;

namespace WmsPharmacyReferral.Controllers
{
    [Host("localhost", "pharmacy.wms-tst.mlcsu.org", "pharmacy.wmp.nhs.uk")]
    public class AuthController : SessionControllerBase
    {
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private readonly ILogger<AuthController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWmsReferralService _WmsSelfReferralService;
        private readonly IODSLookupService _ODSLookupService;
        private TelemetryClient _telemetry;
        private readonly IConfiguration _config;

        public AuthController(ILogger<AuthController> logger, IEmailSender emailSender, IWmsReferralService wmsReferralService,
            IODSLookupService odsLookupService, TelemetryClient telemetry, IConfiguration config)
        {
            _logger = logger;
            _emailSender = emailSender;
            _WmsSelfReferralService = wmsReferralService;
            _ODSLookupService = odsLookupService;
            _telemetry = telemetry;
            _config = config;
        }
        [Route("{controller}/begin")]
        public IActionResult Index()
        {
            var authmodel = GetAuthSessionData();

            if (authmodel.EmailAddress != null)
                return RedirectToAction("validate-code");

            return View(new AuthViewModel() { EmailAddress = authmodel.EmailAddress });
        }
        [Route("{controller}/begin")]
        [HttpPost]
        public async Task<IActionResult> Index(AuthViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.TryGetValue("EmailAddress", out ModelStateEntry emailerror);
                if (emailerror.ValidationState != ModelValidationState.Valid)
                    return View(new AuthViewModel { EmailAddress = model.EmailAddress, Token = "" });
            }

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_NHSNET_EMAIL_DOMAINS);
            if (!validDomain.IsMatch(model.EmailAddress))
            {
                _logger.LogWarning("Email Address not valid: " + model.EmailAddress);
                return View("InvalidEmail");
            }

            var authmodel = GetAuthSessionData(); //if email already validated
            if (authmodel.EmailAddress == model.EmailAddress && authmodel.IsAuthorised)
            {
                return RedirectToAction("lookup-pharmacy");
            }

            //#### Numeric code verification
            //var emailhash = Encoding.ASCII.GetBytes("frrwewq" + model.EmailAddress);
            //var code = Rfc6238AuthenticationService.GenerateCode(emailhash);


            authmodel.EmailAddress = model.EmailAddress;
            authmodel.UserTimeZone = model.UserTimeZone;
            SetAuthSessionData(authmodel);

            var requestedToken = await GenerateToken();
            if (requestedToken.IsTokenGenerated)
            {
                return RedirectToAction("validate-code");
            }
            else
            {
                if (requestedToken.ErrorMessage == "Forbidden")
                {
                    _logger.LogWarning("Email Address denied: " + model.EmailAddress);
                    return View("DeniedEmail");
                }
                if (!ModelState.IsValid)
                {
                    return View(requestedToken);
                }
                return View("GoneWrong", GetErrorModel("Sorry, our services aren't available right now"));
            }

        }
        [AllowAnonymous]
        [Route("{controller}/restart")]
        public IActionResult Restart()
        {
            //clear session
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);

            return Redirect("{controller}/email-address");
        }
        [Route("{controller}/request-new-token")]
        public async Task<IActionResult> RequestNewToken(string emailaddress)
        {
            if (emailaddress == null)
                return RedirectToAction("begin");

            var authmodel = GetAuthSessionData(); //if email already validated/session still alive
            if (authmodel.EmailAddress == null)
            {
                //reassign email address
                authmodel.EmailAddress = emailaddress;
                SetAuthSessionData(authmodel);
            }

            var requestedToken = await GenerateToken();
            if (requestedToken.IsTokenGenerated)
            {
                requestedToken.IsTokenReRequested = true;
                return View("ValidateCode", requestedToken);
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    return View("ValidateCode", requestedToken);
                }
                return View("GoneWrong", GetErrorModel("API Error"));
            }
        }

        private async Task<AuthViewModel> GenerateToken()
        {
            var authmodel = GetAuthSessionData(); //if email already validated
            if (authmodel.EmailAddress != null && authmodel.IsAuthorised)
            {
                authmodel.IsTokenGenerated = true;
                return authmodel;
            }

            try
            {
                //request auth token from API
                int tokenlifetime = _config?.GetValue<int?>("PharmacyReferral:TokenExpiry") ?? 30;
                var authToken = await _WmsSelfReferralService.GenerateOTPTokenAsync(authmodel.EmailAddress, tokenlifetime, "PharmacyReferral");

                if (authToken.IsSuccessStatusCode)
                {
                    var jObj = JObject.Parse(await authToken.Content.ReadAsStringAsync());

                    var token = jObj.SelectToken("$.keyCode")?.Value<string>();
                    var tokenexpiry = jObj.SelectToken("$.expires").Value<DateTime>();
                    var isDST = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time").IsDaylightSavingTime(tokenexpiry);
                    //string utz = "GMT Standard Time";
                    //if (authmodel.UserTimeZone!=null)
                    //    utz = Regex.Match(authmodel.UserTimeZone, @"\((.*?)\)").Value.Replace("(","").Replace(")","");

                    //var utzi = TimeZoneInfo.FindSystemTimeZoneById(utz);
                    //var utokentime = TimeZoneInfo.ConvertTimeFromUtc(tokenexpiry, utzi);

                    DateTime tokenUTC = tokenexpiry.ToUniversalTime();  //CultureInfo culture;
                    if (isDST)
                        tokenUTC = tokenUTC.AddHours(1);

                    await _emailSender.SendEmailAsync(
                        authmodel.EmailAddress,
                        "NHS Digital WMS Pharmacy Referral Token",
                        "<p style='font-family:Arial'>Dear user,</p><p style='font-family:Arial'>Please use the code below to continue using the NHS Digital Weight Management referral service for Pharmacists. <br />The code is single use and will expire at " + tokenUTC.ToShortTimeString() + ".</p><p style='font-family:Arial'><span style='color:white;background-color:black;font-size: 18px;font-weight:bold;display:block;border: solid 20px #000000'>" + token + "</span></p>",
                        "Dear user," + Environment.NewLine + "Please use the code below to continue using the NHS Digital Weight Management referral service for Pharmacists. The code is single use and will expire within 10 minutes." + Environment.NewLine + Environment.NewLine + token
                        );

                    //success
                    authmodel.IsTokenGenerated = true;
                    return authmodel;
                }
                else if (authToken.StatusCode == HttpStatusCode.Forbidden)
                {
                    authmodel.IsTokenGenerated = false;
                    authmodel.ErrorMessage = "Forbidden";
                    return authmodel;
                }
                else if (authToken.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await authToken.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        errors.TryGetValue("Detail", out string detailerror);
                        ModelState.AddModelError("EmailAddress", detailerror);
                        authmodel.ErrorMessage = detailerror;
                        _telemetry.TrackTrace("Email not valid: " + authmodel.EmailAddress, errors);
                    }
                    authmodel.IsTokenGenerated = false;
                    return authmodel;
                }
                else if (authToken.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await authToken.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        errors.TryGetValue("Detail", out string detailerror);
                        ModelState.AddModelError("EmailAddress", detailerror);
                        authmodel.ErrorMessage = detailerror;
                        _telemetry.TrackTrace("Email not valid: " + authmodel.EmailAddress, errors);
                    }
                    authmodel.IsTokenGenerated = false;
                    return authmodel;
                }
                else
                {
                    authmodel.IsTokenGenerated = false;
                    authmodel.ErrorMessage = "API Error";
                    
                    ModelState.Remove("ODSCode");
                    ModelState.Remove("Token");
                    
                    //error, redirect
                    _telemetry.TrackTrace("API Error", new Dictionary<string, string>()
                    {
                        { "Email", authmodel.EmailAddress },
                        { "ODSCode", authmodel.ODSCode },
                        { "ErrorMessage", authmodel.ErrorMessage }
                    });
                    return authmodel;
                }

            }
            catch (Exception ex)
            {
                authmodel.IsTokenGenerated = false;
                authmodel.ErrorMessage = "API Error";

                ModelState.Remove("ODSCode");
                ModelState.Remove("Token");

                //error, redirect
                _telemetry.TrackException(ex, new Dictionary<string, string>()
                {
                    { "Email", authmodel.EmailAddress },
                    { "ODSCode", authmodel.ODSCode },
                    { "ErrorMessage", authmodel.ErrorMessage }
                });

                return authmodel;
            }


        }




        [Route("{controller}/validate-code")]
        public IActionResult ValidateCode()
        {
            var model = GetAuthSessionData();

            if (model.IsAuthorised) //authorised, redirect to ods
                return RedirectToAction("lookup-pharmacy");

            return View(model);
        }
        [Route("{controller}/validate-code")]
        [HttpPost]
        public async Task<IActionResult> ValidateCode(AuthViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.TryGetValue("Token", out ModelStateEntry tokenerror);
                if (tokenerror.ValidationState != ModelValidationState.Valid)
                    return View(model);
            }

            try
            {
                //request auth token from API
                var authToken = await _WmsSelfReferralService.ValidateOTPTokenAsync(model.EmailAddress, model.Token, "PharmacyReferral");

                if (authToken.IsSuccessStatusCode)
                {

                    var jObj = JObject.Parse(await authToken.Content.ReadAsStringAsync());
                    var validtoken = jObj.SelectToken("$.validCode").Value<bool>();
                    if (validtoken) //token is valid
                    {
                        model.IsAuthorised = true;
                        SetAuthSessionData(model);
                        return RedirectToAction("lookup-pharmacy");
                    }


                    return RedirectToAction("validate-code");
                }
                else if (authToken.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await authToken.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        string apierrors = "";
                        if (errors.TryGetValue("Detail", out apierrors))
                            ModelState.AddModelError("Token", apierrors);

                        if (apierrors == null)
                            ModelState.AddModelError("Token", "Error validating the security code.");

                        errors.Add("ODSCode", model.ODSCode);
                        errors.Add("Token", model.Token);
                        _telemetry.TrackTrace("Code not valid for email: " + model.EmailAddress, errors);
                    }

                    return View(model);
                }
                else if (authToken.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await authToken.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        ModelState.AddModelError("Token", "Error validating the security code.");

                        errors.Add("ODSCode", model.ODSCode);
                        errors.Add("Token", model.Token);
                        _telemetry.TrackTrace("Error 400: " + model.EmailAddress, errors);
                    }

                    return View(model);
                }
                else
                {
                    return View("GoneWrong", GetErrorModel("API Error"));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Validate token for " + model.EmailAddress + " failed",
                    new
                    {
                        Email = model.EmailAddress,
                        Token = model.Token
                    });
                return View("GoneWrong", GetErrorModel("API Error"));
            }


        }

        [Route("{controller}/lookup-pharmacy")]
        public IActionResult LookupPharmacy()
        {
            var model = GetAuthSessionData();

            if (!model.IsAuthorised)
            {
                //not auth, return to start
                return RedirectToAction("begin");
            }

            ModelState.Clear();
            return View(model);
        }
        [Route("{controller}/lookup-pharmacy")]
        [HttpPost]
        public async Task<IActionResult> LookupPharmacy(AuthViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.TryGetValue("ODSCode", out ModelStateEntry odserror);
                if (odserror.ValidationState != ModelValidationState.Valid)
                    return View(model);
            }

            //simple ods check
            Regex validODSCode = new Regex(Constants.REGEX_ODSCODE_PHARMACY);
            if (!validODSCode.IsMatch(model.ODSCode))
            {
                _logger.LogWarning("ODS Code not valid: " + model.ODSCode);
                ModelState.AddModelError("ODSCode", "ODS Code not valid");
                return View(model); //not valid                
            }

            //lookup ods code
            var odsOrg = await _ODSLookupService.LookupODSCodeAsync(model.ODSCode);
            if (odsOrg.APIStatusCode == 404)
            {
                _logger.LogWarning($"ODS Code {model.ODSCode} not found");
                ModelState.AddModelError("ODSCode", "ODS Code not found");
                return View(model); //not found
            }
            if (odsOrg.APIStatusCode != 200)
            {
                ModelState.AddModelError("ODSCode", "There is a problem");
                _logger.LogWarning($"ODS Lookup error, code {odsOrg.APIStatusCode}");
                return View(model); //some other error
            }
            if (odsOrg.Status != "Active")
            {
                ModelState.AddModelError("ODSCode", "There is a problem");
                _logger.LogWarning($"ODS, status is not active, status {odsOrg.Status}");
                return View(model); //some other error
            }


            var authmodel = GetAuthSessionData();
            authmodel.ODSOrg = odsOrg;
            authmodel.ODSCode = model.ODSCode.ToUpper();
            SetAuthSessionData(authmodel);


            return RedirectToAction("confirm-pharmacy");
        }
        [Route("{controller}/confirm-pharmacy")]
        public IActionResult ConfirmPharmacy()
        {
            var model = GetAuthSessionData();

            if (!model.IsAuthorised)
            {
                //not auth, return to start
                return RedirectToAction("begin");
            }


            return View(model);
        }

        private AuthViewModel GetAuthSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<AuthViewModel>(SESSION_KEY_EMAIL);
                if (srSession == null) //if its null midway may need to start again
                    return new AuthViewModel { IsAuthorised = false };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new AuthViewModel { };
        }

        private void SetAuthSessionData(AuthViewModel auth)
        {
            try
            {
                HttpContext.Session.Set<AuthViewModel>(SESSION_KEY_EMAIL, auth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        private ErrorViewModel GetErrorModel(string message, string traceid = "")
        {
            return new ErrorViewModel()
            {
                RequestId = "",
                TraceId = traceid,
                Message = $"Error: {message}."
            };
        }



    }
}
