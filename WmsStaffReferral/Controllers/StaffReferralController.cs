using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using WmsStaffReferral.Helpers;
using WmsStaffReferral.Models;

namespace WmsStaffReferral.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class StaffReferralController : SessionControllerBase
    {
        private const string SESSION_KEY_SELFREFERRAL_TOKEN = "StaffReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private const string APIENDPOINT = "StaffReferral";
        private readonly ILogger<StaffReferralController> _logger;
        private readonly IWmsReferralService _WmsSelfReferralService;
        private readonly IPostcodesioService _PostcodesioService;
        private TelemetryClient _telemetry;
        private readonly IWmsCalculations _referralCalcs;

        public StaffReferralController(ILogger<StaffReferralController> logger, IWmsReferralService wmsSelfReferralService, IPostcodesioService postcodesioService, 
            TelemetryClient telemetry, IWmsCalculations referralBusiness)
        {
            _logger = logger;
            _WmsSelfReferralService = wmsSelfReferralService;
            _PostcodesioService = postcodesioService;
            _telemetry = telemetry;
            _referralCalcs = referralBusiness;
        }
        public IActionResult GoneWrong()
        {
            _logger.LogInformation("Gone Wrong View");
            return View();
        }

        [Route("{controller}")]
        public IActionResult Index()
        {

            return RedirectToAction("email-address");
        }
        [Route("{controller}/email-address")]
        public async Task<IActionResult> EmailAddress()
        {
            var authmodel = GetAuthSessionData();
            if (string.IsNullOrEmpty(authmodel.EmailAddress))
                await HttpContext.SignOutAsync();

            if (!string.IsNullOrEmpty(authmodel.EmailAddress))
            {
                //lookup email address with API?
                return RedirectToAction("height");
            }


            var selfReferral = GetReferralSessionData();
            return View(new StaffEmailAddressViewModel { Email = selfReferral.Email });
        }
        [HttpPost]
        [Route("{controller}/email-address")]
        public async Task<IActionResult> EmailAddress(StaffEmailAddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new StaffEmailAddressViewModel { Email = model.Email });
            }

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAIL_DOMAINS);
            if (!validDomain.IsMatch(model.Email))
            {
                return RedirectToAction("not-eligible-for-service", new { id = "Email" });
            }

            try
            {
                //check email against api
                var response = await _WmsSelfReferralService.EmailInUseAsync(model.Email, APIENDPOINT);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await response.Content.ReadAsStringAsync());
                    telemErrors.TryAdd("EmailAddress", model.Email);
                    telemErrors.TryAdd("StatusCode", response.StatusCode.ToString());

                    _telemetry.TrackEvent("GoneWrong:EmailIssue", telemErrors);
                       
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            return View("GoneWrong", GetErrorModel("Email address not valid"));
                        case HttpStatusCode.Conflict:
                            return View(
                              "GoneWrong",
                              GetErrorModel("Thank you. Your referral has already been submitted. " +
                                "If you completed the self-referral form but did not continue to " +
                                "select a provider, you will receive a link via text message to " +
                                "allow you to do this. Please wait up to two working days"));
                        case HttpStatusCode.Unauthorized:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        case HttpStatusCode.InternalServerError:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        default:
                            return View("GoneWrong", GetErrorModel("Email address not valid"));
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent("GoneWrong:API");
            }


            var selfReferral = GetReferralSessionData();
            selfReferral.Email = model.Email;
            SetReferralSessionData(selfReferral);

            //in case this is referral > 1
            ResetReferralSubmitted();

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("height");
        }
        [Route("{controller}/email-address-check")]
        public async Task<IActionResult> EmailAddressCheck()
        {
            var authmodel = GetAuthSessionData();
            if (authmodel.EmailAddress == null)
                await HttpContext.SignOutAsync();



            try
            {
                //check email against api
                var response = await _WmsSelfReferralService.EmailInUseAsync(authmodel.EmailAddress, APIENDPOINT);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await response.Content.ReadAsStringAsync());
                    telemErrors.TryAdd("EmailAddress", authmodel.EmailAddress);
                    telemErrors.TryAdd("StatusCode", response.StatusCode.ToString());

                    _telemetry.TrackEvent("GoneWrong:EmailIssue", telemErrors);

                    HttpContext.Session.Remove(SESSION_KEY_EMAIL);
                    HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
                    await HttpContext.SignOutAsync();
                    

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            return View("GoneWrong", GetErrorModel("Email address not valid"));
                        case HttpStatusCode.Conflict:
                            return View(
                              "GoneWrong",
                              GetErrorModel("Thank you. Your referral has already been submitted. " +
                                "If you completed the self-referral form but did not continue to " +
                                "select a provider, you will receive a link via text message to " +
                                "allow you to do this. Please wait up to two working days"));
                        case HttpStatusCode.Unauthorized:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        case HttpStatusCode.InternalServerError:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        default:
                            return View("GoneWrong", GetErrorModel("Email address not valid"));
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent("GoneWrong:API");
            }


            var selfReferral = GetReferralSessionData();
            selfReferral.Email = authmodel.EmailAddress;
            SetReferralSessionData(selfReferral);

            //in case this is referral > 1
            ResetReferralSubmitted();

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("height");
        }
        [Route("{controller}/height")]
        public IActionResult Height()
        {
            var selfReferral = GetReferralSessionData();
            return View(new HeightViewModel { Height = selfReferral.HeightCm });
        }
        [HttpPost]
        [Route("{controller}/height")]
        public IActionResult Height(HeightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            if (model.Height == null)
            {
                ModelState.TryAddModelError("Height", "Height values not valid");
                return View(model);
            }
            if (model.Height < 50)
            {
                ModelState.TryAddModelError("Height", "Height must be equal or taller than 50cm");
                return View(model);
            }
            if (model.Height > 250)
            {
                ModelState.TryAddModelError("Height", "Height must be equal or lower than 250cm");
                return View(model);
            }

            selfReferral.HeightCm = model.Height;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("weight");
        }
        [Route("{controller}/weight")]
        public IActionResult Weight()
        {
            var selfReferral = GetReferralSessionData();
            if (selfReferral.DateOfBmiAtRegistration == null)
            {
                return View(new WeightViewModel { Weight = selfReferral.WeightKg });
            }
            return View(new WeightViewModel
            {
                Weight = selfReferral.WeightKg,
                Day = selfReferral.DateOfBmiAtRegistration.Value.Day,
                Month = selfReferral.DateOfBmiAtRegistration.Value.Month,
                Year = selfReferral.DateOfBmiAtRegistration.Value.Year
            });
        }
        [HttpPost]
        [Route("{controller}/weight")]
        public IActionResult Weight(WeightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            //weight
            if (model.Weight == null)
            {
                ModelState.AddModelError("WeightError", "Weight values entered are not valid.");
                return View(model);
            }
            if (model.Weight > 500)
            {
                ModelState.AddModelError("WeightError", "Weight must be equal or lower than 500kg");
                return View(model);
            }
            if (model.Weight < 35)
            {
                ModelState.AddModelError("WeightError", "Weight must be equal or greater than 35kg");
                return View(model);
            }

            
            selfReferral.WeightKg = model.Weight;

            //weight date taken
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
            {
                selfReferral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

                if (parsedWeightdate < DateTime.Now.AddMonths(-24))
                {
                    //if past
                    ModelState.AddModelError("DateError", "Date weight was taken must not be older than 2 years");
                    return View(model);
                }
                if (parsedWeightdate > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59))
                {
                    //if future
                    ModelState.AddModelError("DateError", "Date weight was taken must be today or in the past");
                    return View(model);
                }

            }
            else
            {
                //invalid date
                ModelState.AddModelError("DateError", "Date must be a real date");
                return View(model);
            }


            SetReferralSessionData(selfReferral);


            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }
        [Route("{controller}/height-imperial")]
        public IActionResult HeightImperial()
        {
            var selfReferral = GetReferralSessionData();
            return View(_referralCalcs.ConvertCm(selfReferral.HeightCm));
        }
        [HttpPost]
        [Route("{controller}/height-imperial")]
        public IActionResult HeightImperial(HeightImperialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            var heightCm = _referralCalcs.ConvertFeetInches(model.HeightFt, model.HeightIn);
            if (heightCm == null)
            {
                ModelState.AddModelError("HeightError", "Height values not valid");
                return View(model);
            }
            if (heightCm <= 100)
            {
                ModelState.AddModelError("HeightError", "Height must be taller than 3ft 3in");
                return View(model);
            }
            if (heightCm >= 250)
            {
                ModelState.AddModelError("HeightError", "Height must be lower than 8ft 2.4in");
                return View(model);
            }

            selfReferral.HeightCm = heightCm;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("weight-imperial");
        }

        [Route("{controller}/weight-imperial")]
        public IActionResult WeightImperial()
        {
            var selfReferral = GetReferralSessionData();
            var wivm = _referralCalcs.ConvertKg(selfReferral.WeightKg);

            if (selfReferral.DateOfBmiAtRegistration == null)
            {
                return View(wivm);
            }
            return View(new WeightImperialViewModel
            {
                WeightLb = wivm.WeightLb,
                WeightSt = wivm.WeightSt,
                Day = selfReferral.DateOfBmiAtRegistration.Value.Day,
                Month = selfReferral.DateOfBmiAtRegistration.Value.Month,
                Year = selfReferral.DateOfBmiAtRegistration.Value.Year
            });
        }
        [HttpPost]
        [Route("{controller}/weight-imperial")]
        public IActionResult WeightImperial(WeightImperialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            var weight = _referralCalcs.ConvertStonesPounds(model.WeightSt, model.WeightLb);
            if (weight == null)
            {
                ModelState.AddModelError("WeightError", "Weight values entered are not valid.");
                return View(model);
            }
            if (weight > 500)
            {
                ModelState.AddModelError("WeightError", "Weight must be lower than 78st and 10.3lbs");
                return View(model);
            }
            if (weight < 35)
            {
                ModelState.AddModelError("WeightError", "Weight must be greater than 5st and 7.1lbs");
                return View(model);
            }

            selfReferral.WeightKg = weight;

            //weight date taken
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
            {
                selfReferral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

                if (parsedWeightdate < DateTime.Now.AddMonths(-24))
                {
                    //if past
                    ModelState.AddModelError("DateError", "The date your weight was taken must not be older than 2 years");
                    return View(model);
                }
                if (parsedWeightdate > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59))
                {
                    //if future
                    ModelState.AddModelError("DateError", "The date your weight was taken must be today or in the past");
                    return View(model);
                }

            }
            else
            {
                //invalid date
                ModelState.AddModelError("DateError", "Date must be a real date");
                return View(model);
            }

            SetReferralSessionData(selfReferral);


            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }


        [Route("{controller}/weight-date-taken")]
        public IActionResult WeightDateTaken()
        {
            var selfReferral = GetReferralSessionData();

            if (selfReferral.DateOfBmiAtRegistration == null)
                return View();

            return View(new WeightDateTakenViewModel
            {
                Day = selfReferral.DateOfBmiAtRegistration.Value.Day,
                Month = selfReferral.DateOfBmiAtRegistration.Value.Month,
                Year = selfReferral.DateOfBmiAtRegistration.Value.Year
            });
        }
        [HttpPost]
        [Route("{controller}/weight-date-taken")]
        public IActionResult WeightDateTaken(WeightDateTakenViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new WeightDateTakenViewModel
                {
                    Day = model.Day,
                    Month = model.Month,
                    Year = model.Year
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
            {
                selfReferral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

                if (parsedWeightdate < DateTime.Now.AddMonths(-24))
                {
                    //if past
                    ModelState.AddModelError("DateError", "Date weight was taken must not be older than 2 years");
                    return View(model);
                }
                if (parsedWeightdate > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59))
                {
                    //if future
                    ModelState.AddModelError("DateError", "Date weight was taken must be today or in the past");
                    return View(model);
                }

            }
            else
            {
                //invalid date
                ModelState.AddModelError("DateError", "Date must be a real date");
                return View(model);
            }
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }
        [Route("{controller}/select-ethnicity-group")]
        public async Task<IActionResult> EthnicityGroup()
        {
            var selfReferral = GetReferralSessionData();
            var groupName = "";
            if (selfReferral.ServiceUserEthnicityGroup != "")
            {
                //ethnicity has been set already       
                groupName = selfReferral.ServiceUserEthnicityGroup;
            }

            var ethnicities = await _WmsSelfReferralService.GetEthnicityGroupList(APIENDPOINT);
            if (ethnicities.Count() == 0)
            {
                _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", selfReferral.Email }, { "Error", "Ethnicity API null" } });
                //error, redirect
                return View("GoneWrong", GetErrorModel("Service Error"));
            }

            return View(new EthnicityViewModel { EthnicityGroupList = ethnicities, ReferralEthnicityGroup = groupName });
        }
        [HttpPost]
        [Route("{controller}/select-ethnicity-group")]
        public async Task<IActionResult> EthnicityGroup(EthnicityViewModel model)
        {
            ModelState.Remove("ReferralEthnicity");

            if (!ModelState.IsValid)
            {
                return View(new EthnicityViewModel { EthnicityGroupList = await _WmsSelfReferralService.GetEthnicityGroupList(APIENDPOINT), ReferralEthnicityGroup = model.ReferralEthnicityGroup });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                //error, redirect
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            if (model.ReferralEthnicityGroup == "I do not wish to Disclose my Ethnicity")
            {
                selfReferral.Ethnicity = "Other"; //triagename
                selfReferral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup; //groupname
                selfReferral.ServiceUserEthnicity = model.ReferralEthnicityGroup; //displayname
                SetReferralSessionData(selfReferral);

                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

                return RedirectToAction("family-name");
            }

            selfReferral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup;
            SetReferralSessionData(selfReferral);

            return RedirectToAction("Select-Ethnicity", new { id = model.ReferralEthnicityGroup });
        }
        [Route("{controller}/select-ethnicity/")]
        [Route("{controller}/select-ethnicity/{id}")]
        public async Task<IActionResult> Ethnicity(string id)
        {

            var selfReferral = GetReferralSessionData();
            if (id == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:No Ethnicity");
                return View("GoneWrong", GetErrorModel("Something went wrong"));
            }

            return View(new EthnicityViewModel
            {
                EthnicityGroupList = await _WmsSelfReferralService.GetEthnicityGroupList(APIENDPOINT),
                EthnicityGroupDescription = id,
                EthnicityList = await _WmsSelfReferralService.GetEthnicityMembersList(id,APIENDPOINT),
                ReferralEthnicityGroup = id,
                SelectedEthnicity = selfReferral.ServiceUserEthnicity,
                ReferralEthnicity = selfReferral.ServiceUserEthnicity
            });
        }
        [HttpPost]
        [Route("{controller}/select-ethnicity")]
        public async Task<IActionResult> Ethnicity(EthnicityViewModel model)
        {
            ModelState.Remove("ReferralEthnicityGroup");
            if (!ModelState.IsValid)
            {
                return View(new EthnicityViewModel
                {
                    EthnicityGroupList = await _WmsSelfReferralService.GetEthnicityGroupList(APIENDPOINT),
                    EthnicityGroupDescription = model.ReferralEthnicityGroup,
                    EthnicityList = await _WmsSelfReferralService.GetEthnicityMembersList(model.ReferralEthnicityGroup,APIENDPOINT),
                    ReferralEthnicityGroup = model.ReferralEthnicityGroup,
                    SelectedEthnicity = model.ReferralEthnicity
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            var ethnicities = await _WmsSelfReferralService.GetEthnicities(APIENDPOINT);
            if (ethnicities == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", selfReferral.Email }, { "Error", "Ethnicity API null" } });
                return View("GoneWrong", GetErrorModel("Service error"));
            }

            selfReferral.Ethnicity = ethnicities.ToList().Where(w => w.DisplayName == model.ReferralEthnicity).First().TriageName;
            selfReferral.ServiceUserEthnicity = model.ReferralEthnicity; //displayname
            SetReferralSessionData(selfReferral);

            //bmi check
            var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
            if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
            if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("family-name");
        }

        [Route("{controller}/family-name")]
        public IActionResult FamilyName()
        {
            var selfReferral = GetReferralSessionData();
            return View(new FamilyNameViewModel { FamilyName = selfReferral.FamilyName, BackActionRoute = selfReferral.ServiceUserEthnicityGroup });
        }
        [HttpPost]
        [Route("{controller}/family-name")]
        public IActionResult FamilyName(FamilyNameViewModel model)
        {
            var selfReferral = GetReferralSessionData();

            if (!ModelState.IsValid)
            {
                return View(new FamilyNameViewModel { FamilyName = model.FamilyName, BackActionRoute = selfReferral.ServiceUserEthnicityGroup });
            }


            selfReferral.FamilyName = StaticReferralHelper.StringCleaner(model.FamilyName);
            selfReferral.DateOfReferral = DateTime.UtcNow;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("given-name");
        }
        [Route("{controller}/given-name")]
        public IActionResult GivenName()
        {
            var selfReferral = GetReferralSessionData();
            return View(new GivenNameViewModel { GivenName = selfReferral.GivenName });
        }
        [HttpPost]
        [Route("{controller}/given-name")]
        public IActionResult GivenName(GivenNameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.GivenName = StaticReferralHelper.StringCleaner(model.GivenName);
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Address");
        }
        [Route("{controller}/address")]
        public IActionResult Address()
        {
            var selfReferral = GetReferralSessionData();
            return View(new AddressViewModelV1
            {
                Address1 = selfReferral.Address1,
                Address2 = selfReferral.Address2,
                Address3 = selfReferral.Address3,
                Postcode = selfReferral.Postcode

            });
        }

        [HttpPost]
        [Route("{controller}/address")]
        public async Task<IActionResult> Address(AddressViewModelV1 model)
        {
            if (!ModelState.IsValid)
            {
                return View(new AddressViewModelV1
                {
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    Address3 = model.Address3,
                    Postcode = model.Postcode
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            if (model.Postcode != "" && selfReferral.Postcode != model.Postcode.ToUpper())
            {
                //user entered different postcode - revalidate
                model.UserWarned = false;
            }

            selfReferral.Address1 = StaticReferralHelper.StringCleaner(model.Address1);
            selfReferral.Address2 = StaticReferralHelper.StringCleaner(model.Address2);
            selfReferral.Address3 = StaticReferralHelper.StringCleaner(model.Address3);
            selfReferral.Postcode = StaticReferralHelper.StringCleaner(model.Postcode.ToUpper());
            model.Postcode = model.Postcode.ToUpper();
            SetReferralSessionData(selfReferral);

            try
            {
                //validate postcode
                var pccheck = await _PostcodesioService.ValidPostCodeAsync(model.Postcode);
                if (!pccheck && (model.UserWarned == false || model.UserWarned == null))
                {
                    //warn user
                    model.PostCodeValid = false;
                    model.UserWarned = true;
                    ModelState.AddModelError("Postcode", "The postcode entered could not be found.");
                    return View("Address", model);
                }
            }
            catch (Exception ex)
            {
                _telemetry.TrackEvent("Error: PostCodeIO");
                _telemetry.TrackException(ex);
                _logger.LogError(ex.Message);
            }


            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("mobile");
        }
        [Route("{controller}/mobile")]
        public IActionResult Mobile()
        {
            var selfReferral = GetReferralSessionData();
            return View(new MobileViewModel { Mobile = selfReferral.Mobile });
        }
        [HttpPost]
        [Route("{controller}/mobile")]
        public IActionResult Mobile(MobileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }


            selfReferral.Mobile = model.Mobile;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("telephone");
        }
        [Route("{controller}/telephone")]
        public IActionResult Telephone()
        {
            var selfReferral = GetReferralSessionData();
            return View(new TelephoneViewModel { Telephone = selfReferral.Telephone });
        }
        [HttpPost]
        [Route("{controller}/telephone")]
        public IActionResult Telephone(TelephoneViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.Telephone = model.Telephone;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("date-of-birth");
        }

        [Route("{controller}/date-of-birth")]
        public IActionResult DateofBirth()
        {
            var selfReferral = GetReferralSessionData();

            if (selfReferral.DateOfBirth == null)
                return View();

            return View(new DateOfBirthViewModel
            {
                Day = selfReferral.DateOfBirth.Value.Day,
                Month = selfReferral.DateOfBirth.Value.Month,
                Year = selfReferral.DateOfBirth.Value.Year
            });
        }
        [HttpPost]
        [Route("{controller}/date-of-birth")]
        public IActionResult DateofBirth(DateOfBirthViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new DateOfBirthViewModel
                {
                    Day = model.Day,
                    Month = model.Month,
                    Year = model.Year
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dob = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dob, culture, DateTimeStyles.None, out DateTime outdob))
            {
                selfReferral.DateOfBirth = new DateTimeOffset(outdob.Year, outdob.Month, outdob.Day, 0, 0, 0, TimeSpan.Zero);
                var age = _referralCalcs.CalcAge(selfReferral);
                if (age < 18)
                {
                    //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "Age" });
                }
                else if (age > 110)
                {
                    //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "Age" });
                }

            }
            else
            {
                ModelState.AddModelError("DateError", "Date must be a real date");
                return View(model);
            }
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Sex");
        }
        [Route("{controller}/sex")]
        public IActionResult Sex()
        {
            var selfReferral = GetReferralSessionData();
            return View(new SexViewModel { Sex = selfReferral.Sex, Sexes = StaticReferralHelper.GetSexes() });
        }
        [HttpPost]
        [Route("{controller}/sex")]
        public IActionResult Sex(SexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new SexViewModel { Sex = model.Sex, Sexes = StaticReferralHelper.GetSexes() });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.Sex = model.Sex;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Staff-Role");
        }
        [Route("{controller}/staff-role")]
        public async Task<IActionResult> StaffRole()
        {
            var selfReferral = GetReferralSessionData();
            return View(new StaffRoleViewModel
            {
                StaffRole = selfReferral.StaffRole,
                StaffRoleList = await GetStaffRoleList()
            });
        }
        [HttpPost]
        [Route("{controller}/staff-role")]
        public async Task<IActionResult> StaffRole(StaffRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new StaffRoleViewModel
                {
                    StaffRole = model.StaffRole,
                    StaffRoleList = await GetStaffRoleList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.StaffRole = model.StaffRole;
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("consent-for-future-contact");
        }
        [Route("{controller}/consent-for-future-contact")]
        public IActionResult ConsentFutureContact()
        {
            var selfReferral = GetReferralSessionData();
            return View(new ConsentFutureContactViewModel
            {
                FutureContact = selfReferral.ConsentForFutureContactForEvaluation == null
                ? "null" : selfReferral.ConsentForFutureContactForEvaluation == true
                ? "true" : "false",
                FutureContactList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null").ToList()
            });
        }
        [HttpPost]
        [Route("{controller}/consent-for-future-contact")]
        public IActionResult ConsentFutureContact(WmsReferral.Business.Models.ConsentFutureContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new WmsReferral.Business.Models.ConsentFutureContactViewModel
                {
                    FutureContact = model.FutureContact,
                    FutureContactList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null").ToList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }


            selfReferral.ConsentForFutureContactForEvaluation = model.FutureContact == "true";
            SetReferralSessionData(selfReferral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("physical-disability");
        }
        [Route("{controller}/physical-disability")]
        public IActionResult PhysicalDisability()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new PhysicalDisabilityViewModel
            {
                PhysicalDisability = keyAnswers.AnsweredPhysicalDisability ? selfReferral.HasAPhysicalDisability == null ? "null" : selfReferral.HasAPhysicalDisability == true ? "true" : "false" : "",
                PhysicalDisabilityList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/physical-disability")]
        public IActionResult PhysicalDisability(PhysicalDisabilityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new PhysicalDisabilityViewModel
                {
                    PhysicalDisability = model.PhysicalDisability,
                    PhysicalDisabilityList = StaticReferralHelper.GetYNList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.HasAPhysicalDisability = model.PhysicalDisability == "true" ? true : model.PhysicalDisability == "false" ? false : null;
            SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredPhysicalDisability = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Learning-Disability");
        }
        [Route("{controller}/learning-disability")]
        public IActionResult LearningDisability()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new LearningDisabilityViewModel
            {
                LearningDisability = keyAnswers.AnsweredLearningDisability ? selfReferral.HasALearningDisability == null ? "null" : selfReferral.HasALearningDisability == true ? "true" : "false" : "",
                LearningDisabilityList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/learning-disability")]
        public IActionResult LearningDisability(LearningDisabilityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new LearningDisabilityViewModel
                {
                    LearningDisability = model.LearningDisability,
                    LearningDisabilityList = StaticReferralHelper.GetYNList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.HasALearningDisability = model.LearningDisability == "true" ? true : model.LearningDisability == "false" ? false : null;
            SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredLearningDisability = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Hypertension");
        }
        [Route("{controller}/hypertension")]
        public IActionResult Hypertension()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new HypertensionViewModel
            {
                Hypertension = keyAnswers.AnsweredHypertension ? selfReferral.HasHypertension == null ? "null" : selfReferral.HasHypertension == true ? "true" : "false" : "",
                HypertensionList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/hypertension")]
        public IActionResult Hypertension(HypertensionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new HypertensionViewModel
                {
                    Hypertension = model.Hypertension,
                    HypertensionList = StaticReferralHelper.GetYNList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.HasHypertension = model.Hypertension == "true" ? true : model.Hypertension == "false" ? false : null;
            SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredHypertension = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Diabetes-Type-One");
        }
        [Route("{controller}/diabetes-type-one")]
        public IActionResult DiabetesTypeOne()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new DiabetesViewModel
            {
                DiabetesTypeOne = keyAnswers.AnsweredDiabetesType1 ? selfReferral.HasDiabetesType1 == null ? "null" : selfReferral.HasDiabetesType1 == true ? "true" : "false" : "",
                DiabetesList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/diabetes-type-one")]
        public IActionResult DiabetesTypeOne(DiabetesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new DiabetesViewModel
                {
                    DiabetesTypeOne = model.DiabetesTypeOne,
                    DiabetesList = StaticReferralHelper.GetYNList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.HasDiabetesType1 = model.DiabetesTypeOne == "true" ? true : model.DiabetesTypeOne == "false" ? false : null;
            SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType1 = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("diabetes-type-two");
        }
        [Route("{controller}/diabetes-type-two")]
        public IActionResult DiabetesTypeTwo()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();

            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            return View(new DiabetesTypeTwoViewModel
            {
                DiabetesTypeTwo = keyAnswers.AnsweredDiabetesType2 ? selfReferral.HasDiabetesType2 == null ? "null" : selfReferral.HasDiabetesType2 == true ? "true" : "false" : "",
                DiabetesList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/diabetes-type-two")]
        public IActionResult DiabetesTypeTwo(DiabetesTypeTwoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new DiabetesTypeTwoViewModel
                {
                    DiabetesTypeTwo = model.DiabetesTypeTwo,
                    DiabetesList = StaticReferralHelper.GetYNList()
                });
            }

            var selfReferral = GetReferralSessionData();
            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            selfReferral.HasDiabetesType2 = model.DiabetesTypeTwo == "true" ? true : model.DiabetesTypeTwo == "false" ? false : null;
            SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType2 = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("check-answers");
        }

        [Route("{controller}/check-answers")]
        public IActionResult CheckAnswers()
        {
            var selfReferral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();

            if (keyAnswers.ReferralSubmitted
                && !keyAnswers.ProviderChoiceSubmitted)
            {
                return RedirectToAction("provider-choice");
            }
            else if (keyAnswers.ReferralSubmitted
                && keyAnswers.ProviderChoiceSubmitted)
            {
                //error, already submitted
                _telemetry.TrackEvent("GoneWrong:Already submitted");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted"));
            }

            if (selfReferral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            //set imperial
            keyAnswers.HeightImperial = _referralCalcs.ConvertCm(selfReferral.HeightCm) ?? keyAnswers.HeightImperial;
            keyAnswers.WeightImperial = _referralCalcs.ConvertKg(selfReferral.WeightKg) ?? keyAnswers.WeightImperial;
            SetAnswerSessionData(keyAnswers);

            if (keyAnswers.HeightImperial == null || keyAnswers.WeightImperial == null)
            {
                //imperial units are null, likely a timeout or user has completed and is retrying
                _telemetry.TrackEvent("GoneWrong:Not complete");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted or is incomplete"));
            }

            if (!ReferralCompleted())
            {
                _telemetry.TrackEvent("GoneWrong:Not complete");
                return View("GoneWrong", GetErrorModel("Referral is incomplete, please try again"));
            }


            return View(new CheckAnswersViewModel { Referral = selfReferral, KeyAnswer = keyAnswers });
        }
        [HttpPost]
        [Route("{controller}/check-answers")]
        public async Task<IActionResult> CheckAnswers(CheckAnswersViewModel model)
        {
            try
            {
                var selfReferral = GetReferralSessionData();
                selfReferral = (StaffReferral)StaticReferralHelper.FinalCheckAnswerChecks(selfReferral);

                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

                //post to api
                var result = await _WmsSelfReferralService.AddStaffReferralAsync(selfReferral);

                //if success
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    var referral = JsonConvert.DeserializeObject<ProviderChoiceModel>(await result.Content.ReadAsStringAsync());
                    //add additional choice
                    referral.ProviderChoices.Add(new Provider()
                    {
                        Id = new Guid("2021c46b-ce2a-4e0d-8dd9-c38af0813ade"),
                        Name = "Need more time to decide?",
                        Summary = "If you need more time to choose a service, select this option and we'll send you a text message within 2 working days. This message, from ‘NHS Service’, will contain a link to a website from which you can access the list of available programmes. Please note that the link will expire in 48 hours."

                    });
                    SetProviderChoiceSessionData(referral);

                    //mark referral submitted
                    var keyAnswers = GetAnswerSessionData();
                    keyAnswers.ReferralSubmitted = true;
                    SetAnswerSessionData(keyAnswers);

                    return RedirectToAction("provider-choice");
                }


                //problem
                var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await result.Content.ReadAsStringAsync(), selfReferral);

                switch (result.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        //missing/invalid values, check errors                        
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel(telemErrors.GetValueOrDefault("Error"), telemErrors.GetValueOrDefault("TraceId")));
                    case HttpStatusCode.Unauthorized:
                        //problem with api key
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Not authorised"));
                    case HttpStatusCode.Forbidden:
                        //invalid referral exists
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Referral already exists", telemErrors.GetValueOrDefault("TraceId")));
                    case HttpStatusCode.Conflict:
                        //referral already exists
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Referral already exists", telemErrors.GetValueOrDefault("TraceId")));
                    default:
                        //some other error, e.g. internal error
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel(result.StatusCode.ToString() + " An error has occured."));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error submitting referral.");
                return View("GoneWrong", GetErrorModel(500 + " An error has occured."));
            }
        }

        [Route("{controller}/provider-choice")]
        public IActionResult ProviderChoice()
        {
            if (ReferralSubmitted())
            {
                //error, already submitted
                _telemetry.TrackEvent("GoneWrong:Already submitted");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted"));
            }

            var providerChoice = GetProviderChoiceSessionData();
            if (providerChoice.Id == Guid.Empty)
            {
                //gonewrong
                _telemetry.TrackEvent("GoneWrong: ProviderChoice lost");
                return View("GoneWrong", GetErrorModel("An error has occured."));
            }


            return View(providerChoice);
        }
        [HttpPost]
        [Route("{controller}/provider-choice")]
        public IActionResult ProviderChoice(ProviderChoiceModel model)
        {
            var providerChoice = GetProviderChoiceSessionData();
            model.ProviderChoices = providerChoice.ProviderChoices;
            model.Id = providerChoice.Id;

            if (ReferralSubmitted())
            {
                //error, already submitted
                _telemetry.TrackEvent("GoneWrong:Already submitted");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted"));
            }

            if (!ModelState.IsValid)
                return View(model);
            if (model.ProviderId == Guid.Empty)
            {
                ModelState.AddModelError("ProviderId", "A service must be selected.");
                return View(model);
            }


            if (providerChoice.Id == Guid.Empty)
            {
                //gonewrong
                _telemetry.TrackEvent("GoneWrong: ProviderChoice lost");
                return View("GoneWrong", GetErrorModel("An error has occured."));
            }

            providerChoice.ProviderId = model.ProviderId;
            providerChoice.Provider = model.ProviderChoices
                .Where(w => w.Id == model.ProviderId).First();
            SetProviderChoiceSessionData(providerChoice);


            return View("providerconfirm", providerChoice);
        }

        [HttpPost]
        [Route("{controller}/provider-confirm")]
        public async Task<IActionResult> ProviderConfirm(ProviderChoiceModel model)
        {
            try
            {
                var providerChoice = GetProviderChoiceSessionData();
                model.ProviderId = model.Provider.Id;
                model.ProviderChoices = providerChoice.ProviderChoices;
                model.Id = providerChoice.Id;

                if (ReferralSubmitted())
                {
                    //error, already submitted
                    _telemetry.TrackEvent("GoneWrong:Already submitted");
                    return View("GoneWrong", GetErrorModel("Referral has already been submitted"));
                }

                if (providerChoice.Id == Guid.Empty)
                {
                    //gonewrong
                    _telemetry.TrackEvent("GoneWrong: ProviderChoice lost");
                    return View("GoneWrong", GetErrorModel("An error has occured."));
                }

                //user wants more time
                if (model.ProviderId == new Guid("2021c46b-ce2a-4e0d-8dd9-c38af0813ade"))
                    return RedirectToAction("complete", new { id = "NoChoice" });

                //post to api
                var result = await _WmsSelfReferralService.UpdateProviderChoiceAsync(model);

                //success
                if (result.StatusCode == HttpStatusCode.OK)
                    return RedirectToAction("complete", new { id = "Complete" });

                //problem
                var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await result.Content.ReadAsStringAsync());

                switch (result.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        //missing/invalid values, check errors                        
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel(telemErrors.GetValueOrDefault("Error"), telemErrors.GetValueOrDefault("TraceId")));
                    case HttpStatusCode.Unauthorized:
                        //problem with api key
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Not authorised"));
                    case HttpStatusCode.Forbidden:
                        //invalid referral exists
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Referral already exists", telemErrors.GetValueOrDefault("TraceId")));
                    case HttpStatusCode.Conflict:
                        //referral already exists
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel("Referral already exists", telemErrors.GetValueOrDefault("TraceId")));
                    default:
                        //some other error, e.g. internal error
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel(result.StatusCode.ToString() + " An error has occured."));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error updating referral.");
                return View("GoneWrong", GetErrorModel(500 + " An error has occured."));
            }
        }

        [Route("{controller}/complete")]
        public IActionResult Complete(string id)
        {
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);

            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType1 = false;
            keyAnswers.AnsweredDiabetesType2 = false;
            keyAnswers.AnsweredLearningDisability = false;
            keyAnswers.AnsweredHypertension = false;
            keyAnswers.AnsweredPhysicalDisability = false;
            //mark providerchoice submitted
            keyAnswers.ProviderChoiceSubmitted = true;
            SetAnswerSessionData(keyAnswers);


            if (id == "Complete")
            {
                return View("Complete");
            }
            else
            {
                return View("CompleteNoChoice");
            }
        }
        [Route("{controller}/not-eligible-for-service/{id}")]
        public IActionResult NotEligible(string id)
        {

            var selfReferral = GetReferralSessionData();
            _telemetry.TrackEvent("NotEligible", new Dictionary<string, string> {
                { "Age",_referralCalcs.CalcAge(selfReferral).ToString() ?? "NotSet" },
                { "BMI",_referralCalcs.CalcBmi(selfReferral).ToString() ?? "NotSet" },
                { "Reason", id }
            });

            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            return id switch
            {
                "BMI" => View("NotEligibleBMI", new NotEligibleViewModel { Message = "" }),
                "BMI-Limit" => View("NotEligibleBMILimit", new NotEligibleViewModel { Message = "" }),
                "Email" => View("NotEligibleEmail", new NotEligibleViewModel { Message = "" }),
                "Age" => View("NotEligibleAge", new NotEligibleViewModel { Message = "" }),
                _ => View(new NotEligibleViewModel { Message = "" }),
            };
        }
        [Route("{controller}/session-ping")]
        public IActionResult SessionPing()
        {
            return Ok();
        }
       

        private async Task<List<KeyValuePair<string, string>>> GetStaffRoleList()
        {
            List<KeyValuePair<string, string>> staffRoleList = new();

            // use a service to get a list of ethnicity codes
            try
            {
                IEnumerable<StaffRole> businessModel =
                  await _WmsSelfReferralService.GetStaffRolesAsync();

                foreach (string staffRole in businessModel.OrderBy(o => o.DisplayOrder).Select(s => s.DisplayName))
                {
                    staffRoleList.Add(new KeyValuePair<string, string>(staffRole,staffRole));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return staffRoleList.ToList();
        }
       
        private StaffReferral GetReferralSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<StaffReferral>(SESSION_KEY_SELFREFERRAL_TOKEN);
                if (srSession == null) //if its null midway may need to start again
                    return new StaffReferral { ReferringGpPracticeNumber = "V81998" };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new StaffReferral { ReferringGpPracticeNumber = "V81998" };
        }

        private void SetReferralSessionData(StaffReferral selfReferral)
        {
            try
            {
                HttpContext.Session.Set<StaffReferral>(SESSION_KEY_SELFREFERRAL_TOKEN, selfReferral);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        private bool ReferralCompleted()
        {
            var answers = GetAnswerSessionData();
            if (answers.AnsweredDiabetesType1 == true
                && answers.AnsweredDiabetesType2 == true
                && answers.AnsweredHypertension == true
                && answers.AnsweredLearningDisability == true
                && answers.AnsweredPhysicalDisability == true
                )
            {
                return true;
            }

            return false;
        }
        private bool ReferralSubmitted()
        {
            var answers = GetAnswerSessionData();
            if (answers.ReferralSubmitted && answers.ProviderChoiceSubmitted)
            {
                return true;
            }

            return false;
        }
        private void ResetReferralSubmitted()
        {
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.ReferralSubmitted = false;
            SetAnswerSessionData(keyAnswers);
        }

        private KeyAnswer GetAnswerSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<KeyAnswer>(SESSION_KEY_ANSWER_TOKEN);
                if (srSession == null) //if its null midway may need to start again
                    return new KeyAnswer
                    {
                        AnsweredDiabetesType1 = false,
                        AnsweredDiabetesType2 = false,
                        AnsweredHypertension = false,
                        AnsweredLearningDisability = false,
                        AnsweredPhysicalDisability = false,
                        ReferralSubmitted = false,
                        ProviderChoiceSubmitted = false
                    };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session");
            }

            //error
            return new KeyAnswer
            {
                AnsweredDiabetesType1 = false,
                AnsweredDiabetesType2 = false,
                AnsweredHypertension = false,
                AnsweredLearningDisability = false,
                AnsweredPhysicalDisability = false,
                ReferralSubmitted = false,
                ProviderChoiceSubmitted = false
            };
        }
        private void SetAnswerSessionData(KeyAnswer keyAnswer)
        {
            try
            {
                HttpContext.Session.Set<KeyAnswer>(SESSION_KEY_ANSWER_TOKEN, keyAnswer);
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
        
        private ProviderChoiceModel GetProviderChoiceSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<ProviderChoiceModel>(SESSION_PROVIDER_CHOICE_TOKEN);
                if (srSession == null) //if its null midway may need to start again
                    return new ProviderChoiceModel
                    {

                    };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new ProviderChoiceModel
            {

            };
        }
        private void SetProviderChoiceSessionData(ProviderChoiceModel model)
        {
            try
            {
                HttpContext.Session.Set<ProviderChoiceModel>(SESSION_PROVIDER_CHOICE_TOKEN, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

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
    }




}
