using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsStaffReferral.Helpers;
using WmsStaffReferral.Models;

namespace WmsStaffReferral.Controllers
{
    public class OTPAuthController : SessionControllerBase
    {
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private readonly ILogger<OTPAuthController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWmsReferralService _WmsSelfReferralService;        
        private TelemetryClient _telemetry;
        private readonly IConfiguration _config;

        public OTPAuthController(ILogger<OTPAuthController> logger, IEmailSender emailSender, IWmsReferralService wmsReferralService,
            TelemetryClient telemetry, IConfiguration config)
        {
            _logger = logger;
            _emailSender = emailSender;
            _WmsSelfReferralService = wmsReferralService;            
            _telemetry = telemetry;
            _config = config;
        }

        [AllowAnonymous]
        [Route("{controller}/")]
        [Route("{controller}/begin")]
        [Route("{controller}/index")]
        [Route("{controller}/login")]
        [Route("{controller}/email-address")]
        public IActionResult Email()
        {
            var authmodel = GetAuthSessionData();

            if (authmodel.EmailAddress != null && !authmodel.IsAuthorised)
                return RedirectToAction("validate-code");

            return View(new OTPAuthViewModel() { EmailAddress = authmodel.EmailAddress });
        }
        [AllowAnonymous]
        [Route("{controller}/email-address")]
        [HttpPost]
        public async Task<IActionResult> Email(OTPAuthViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.TryGetValue("EmailAddress", out ModelStateEntry emailerror);
                if (emailerror.ValidationState != ModelValidationState.Valid)
                    return View(new OTPAuthViewModel { EmailAddress = model.EmailAddress, Token = "" });
            }

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAIL_DOMAINS);
            if (!validDomain.IsMatch(model.EmailAddress))
            {
                _logger.LogWarning("Email Address not valid: " + model.EmailAddress);
                return View("InvalidEmail");
            }
                       

            var authmodel = GetAuthSessionData(); //if email already validated
            if (authmodel.EmailAddress == model.EmailAddress && authmodel.IsAuthorised)
            {
                return RedirectToAction("height", "StaffReferral");
            }

            
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


        private async Task<OTPAuthViewModel> GenerateToken()
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
                int tokenlifetime = _config?.GetValue<int?>("WmsStaffReferral:TokenExpiry") ?? 30;
                var authToken = await _WmsSelfReferralService.GenerateOTPTokenAsync(authmodel.EmailAddress, tokenlifetime, "StaffReferral");

                if (authToken.IsSuccessStatusCode)
                {
                    var jObj = JObject.Parse(await authToken.Content.ReadAsStringAsync());
                    var token = jObj.SelectToken("$.keyCode")?.Value<string>();
                    var tokenexpiry = jObj.SelectToken("$.expires")?.Value<DateTime>();
                    if (tokenexpiry != null)
                    {
                        var isDST = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time").IsDaylightSavingTime(tokenexpiry.Value);
                        DateTime tokenUTC = tokenexpiry.Value.ToUniversalTime();  //CultureInfo culture;
                        if (isDST)
                            tokenUTC = tokenUTC.AddHours(1);

                        await _emailSender.SendEmailAsync(
                            authmodel.EmailAddress,
                            "NHS Digital WMP Staff Referral Token",
                            "<p style='font-family:Arial'>Dear user,</p><p style='font-family:Arial'>Please use the code below to continue using the NHS Digital Weight Management referral service for NHS Staff. <br />The code is single use and will expire at " + tokenUTC.ToShortTimeString() + ".</p><p style='font-family:Arial'><span style='color:white;background-color:black;font-size: 18px;font-weight:bold;display:block;border: solid 20px #000000'>" + token + "</span></p>",
                            "Dear user," + Environment.NewLine + "Please use the code below to continue using the NHS Digital Weight Management referral service for NHS Staff. The code is single use and will expire within 10 minutes." + Environment.NewLine + Environment.NewLine + token
                            );

                        //success
                        authmodel.IsTokenGenerated = true;
                    }
                    else
                    {
                        //success
                        authmodel.IsTokenGenerated = false;
                        authmodel.ErrorMessage = "Date issue";
                    }
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
                    { "ErrorMessage", authmodel.ErrorMessage }
                });

                return authmodel;
            }


        }



        [AllowAnonymous]
        [Route("{controller}/validate-code")]
        public IActionResult ValidateCode()
        {
            var model = GetAuthSessionData();

            if (model.IsAuthorised) //authorised, redirect to ods
                return RedirectToAction("email-address-check", "StaffReferral");

            return View(model);
        }
        [AllowAnonymous]
        [Route("{controller}/validate-code")]
        [HttpPost]
        public async Task<IActionResult> ValidateCode(OTPAuthViewModel model)
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
                var authToken = await _WmsSelfReferralService.ValidateOTPTokenAsync(model.EmailAddress, model.Token, "StaffReferral");

                if (authToken.IsSuccessStatusCode)
                {

                    var jObj = JObject.Parse(await authToken.Content.ReadAsStringAsync());
                    var validtoken = jObj.SelectToken("$.validCode").Value<bool>();
                    if (validtoken) //token is valid
                    {
                        //model.IsAuthorised = true;
                        //SetAuthSessionData(model);
                        //Login the user
                        await Login(model);

                        return RedirectToAction("EmailAddressCheck", "StaffReferral");
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

        [AllowAnonymous]
        [Route("{controller}/restart")]
        public IActionResult Restart()
        {
            //clear session
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);

            return Redirect("/OTPAuth/email-address");
        }

        private async Task Login(OTPAuthViewModel model)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.EmailAddress),
                new Claim(ClaimTypes.NameIdentifier, model.EmailAddress),                
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                //AllowRefresh = <bool>,
                // Refreshing the authentication session should be allowed.

                //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                // The time at which the authentication ticket expires. A 
                // value set here overrides the ExpireTimeSpan option of 
                // CookieAuthenticationOptions set with AddCookie.

                //IsPersistent = true,
                // Whether the authentication session is persisted across 
                // multiple requests. When used with cookies, controls
                // whether the cookie's lifetime is absolute (matching the
                // lifetime of the authentication ticket) or session-based.

                //IssuedUtc = <DateTimeOffset>,
                // The time at which the authentication ticket was issued.

                //RedirectUri = <string>
                // The full path or absolute URI to be used as an http 
                // redirect response value.
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Email} logged in at {Time}.",
                model.EmailAddress, DateTime.UtcNow);


            model.IsAuthorised = true;
            SetAuthSessionData(model);


        }


        private OTPAuthViewModel GetAuthSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<OTPAuthViewModel>(SESSION_KEY_EMAIL);
                if (srSession == null) //if its null midway may need to start again
                    return new OTPAuthViewModel { IsAuthorised = false };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new OTPAuthViewModel { };
        }

        private void SetAuthSessionData(OTPAuthViewModel auth)
        {
            try
            {
                HttpContext.Session.Set<OTPAuthViewModel>(SESSION_KEY_EMAIL, auth);
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
