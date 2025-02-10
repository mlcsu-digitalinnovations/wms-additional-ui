using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WmsMskReferral.Helpers;
using WmsMskReferral.Models;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;

namespace WmsMskReferral.Controllers
{

    public class OTPAuthController : SessionControllerBase
    {
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private readonly ILogger<OTPAuthController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWmsReferralService _WmsSelfReferralService;
        private readonly IODSLookupService _ODSLookupService;
        private TelemetryClient _telemetry;
        private readonly IConfiguration _config;
        public OTPAuthController(ILogger<OTPAuthController> logger, IEmailSender emailSender, IWmsReferralService wmsReferralService,
            IODSLookupService odsLookupService, TelemetryClient telemetry, IConfiguration config)
        {
            _logger = logger;
            _emailSender = emailSender;
            _WmsSelfReferralService = wmsReferralService;
            _ODSLookupService = odsLookupService;
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

            if (authmodel.EmailAddress != null && !authmodel.IsAuthorised && !authmodel.EmailAddress.Contains("@nhs.net"))
                return RedirectToAction("validate-code");

            return View(new MskHubViewModel() { EmailAddress = authmodel.EmailAddress });
        }
        [AllowAnonymous]
        [Route("{controller}/email-address")]
        [HttpPost]
        public async Task<IActionResult> Email(MskHubViewModel model)
        {
            ModelState.Remove("ODSOrg");
            ModelState.Remove("ODSCode");
            ModelState.Remove("MskHubList");

            if (!ModelState.IsValid)
            {
                ModelState.TryGetValue("EmailAddress", out var emailerror);
                if (emailerror?.ValidationState != ModelValidationState.Valid)
                    return View(new MskHubViewModel { EmailAddress = model.EmailAddress, Token = "" });
            }
            if (model.EmailAddress == null)
                return View(new MskHubViewModel { EmailAddress = model.EmailAddress, Token = "" });

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAIL_DOMAINS);
            if (!validDomain.IsMatch(model.EmailAddress))
            {
                _logger.LogWarning("Email Address not valid: " + model.EmailAddress);
                return View("InvalidEmail");
            }

            //nhsmail check, redirect to nhsmail auth
            Regex nhsmailreg = new Regex(Constants.REGEX_WMS_NHSNET_EMAIL_DOMAINS);
            if (nhsmailreg.IsMatch(model.EmailAddress))
            {
                _logger.LogWarning("Email Address is nhsmail via OTP: " + model.EmailAddress);

                var properties = new AuthenticationProperties();
                properties.SetParameter("login_hint", model.EmailAddress);

                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                return View();
            }

            var authmodel = GetAuthSessionData(); //if email already validated
            if (authmodel.EmailAddress == model.EmailAddress && authmodel.IsAuthorised)
            {
                var authUser = User.Identity?.IsAuthenticated;
                if (authUser == false)
                {
                    //not authorised but does have a session??? reset
                    _logger.LogWarning("User has session, but not authorised. Clearing session. " + model.EmailAddress);
                    HttpContext.Session.Remove(SESSION_KEY_EMAIL);
                    return RedirectToAction("Login", "OTPAuth");
                }


                return RedirectToAction("MskHub", "MskHub");
            }

            //set session
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
                //clear session
                HttpContext.Session.Remove(SESSION_KEY_EMAIL);

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


        private async Task<MskHubViewModel> GenerateToken()
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
                int tokenlifetime = _config?.GetValue<int?>("WmsMskReferral:TokenExpiry") ?? 30;
                var authToken = await _WmsSelfReferralService.GenerateOTPTokenAsync(authmodel.EmailAddress, tokenlifetime, "MskReferral");

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
                            "NHS Digital WMP MSK Referral Token",
                            "<p style='font-family:Arial'>Dear user,</p><p style='font-family:Arial'>Please use the code below to continue using the NHS Digital Weight Management referral service for MSK. <br />The code is single use and will expire at " + tokenUTC.ToShortTimeString() + ".</p><p style='font-family:Arial'><span style='color:white;background-color:black;font-size: 18px;font-weight:bold;display:block;border: solid 20px #000000'>" + token + "</span></p>",
                            "Dear user," + Environment.NewLine + "Please use the code below to continue using the NHS Digital Weight Management referral service for MSK. The code is single use and will expire within 10 minutes." + Environment.NewLine + Environment.NewLine + token
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
                        errors.TryGetValue("Detail", out var detailerror);
                        ModelState.AddModelError("EmailAddress", detailerror ?? "Not valid");
                        authmodel.ErrorMessage = detailerror ?? "Not valid";
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
                        errors.TryGetValue("Detail", out var detailerror);
                        ModelState.AddModelError("EmailAddress", detailerror ?? "Not valid");
                        authmodel.ErrorMessage = detailerror ?? "Not valid";
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
                        { "Email", authmodel.EmailAddress ?? "" },
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



        [AllowAnonymous]
        [Route("{controller}/validate-code")]
        public IActionResult ValidateCode()
        {
            var model = GetAuthSessionData();

            if (model.IsAuthorised) //authorised, redirect to ods
                return RedirectToAction("select-msk-hub", "MskHub");

            return View(model);
        }
        [AllowAnonymous]
        [Route("{controller}/validate-code")]
        [HttpPost]
        public async Task<IActionResult> ValidateCode(MskHubViewModel model)
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
                var authToken = await _WmsSelfReferralService.ValidateOTPTokenAsync(model.EmailAddress, model.Token, "MskReferral");

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

                        //check if they've previously made a choice
                        var ODSCodeCookie = GetODSCookie();
                        if (ODSCodeCookie != "None")
                        {
                            //must be a valid ODS available via API

                            try
                            {

                                var mskhubs = await _WmsSelfReferralService.GetMskHubs();
                                if (mskhubs == null)
                                {
                                    _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", model.EmailAddress }, { "Error", "MskHub API null" } });
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

                            if (model.MskHubList.Where(w => w.OdsCode == ODSCodeCookie).Any())
                            {
                                model.ODSCode = ODSCodeCookie;
                                model.SelectedMskHub = ODSCodeCookie;
                                model.NameIdentifier = model.EmailAddress ?? "";
                                var odsorg = await _ODSLookupService.LookupODSCodeAsync(model.ODSCode);
                                if (odsorg != null)
                                    model.ODSOrg = odsorg;

                                SetAuthSessionData(model);

                                return View("MskHubConfirm", model);
                            }

                        }

                        return RedirectToAction("select-msk-hub", "MskHub");
                    }


                    return RedirectToAction("validate-code");
                }
                else if (authToken.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await authToken.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        var apierrors = "";
                        if (errors.TryGetValue("Detail", out apierrors))
                            ModelState.AddModelError("Token", apierrors ?? "Error");

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

        [Route("{controller}/select-msk-hub")]
        public IActionResult MskHub()
        {
            return RedirectToAction("select-msk-hub", "MskHub");
        }

        [AllowAnonymous]
        [Route("{controller}/restart")]
        public IActionResult Restart()
        {
            //clear session
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);

            return Redirect("/OTPAuth/email-address");
        }

        private async Task Login(MskHubViewModel model)
        {
            if (model.EmailAddress != null)
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.EmailAddress),
                new Claim(ClaimTypes.NameIdentifier, model.EmailAddress),
                //new Claim("ODS", model.ODSCode),
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
            else
            {
                _logger.LogInformation("User {Email} not logged in at {Time}. No email address set.",
                   model.EmailAddress, DateTime.UtcNow);


                model.IsAuthorised = false;
                SetAuthSessionData(model);
            }

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
