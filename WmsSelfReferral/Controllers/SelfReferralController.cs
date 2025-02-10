using IdentityModel.Client;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using WmsSelfReferral.Data;
using WmsSelfReferral.Helpers;
using WmsSelfReferral.Models;

namespace WmsSelfReferral.Controllers
{
    [Authorize]
    public class SelfReferralController : SessionControllerBase
    {
        private const string SESSION_KEY_SELFREFERRAL_TOKEN = "SelfReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";
        private const string APIENDPOINT = "GeneralReferral";
        private readonly ILogger<SelfReferralController> _logger;
        private readonly IWmsReferralService _WmsSelfReferralService;
        private readonly IPostcodesioService _PostcodesioService;
        private readonly IGetAddressioService _GetAddressioService;
        private readonly TelemetryClient _telemetry;
        private readonly IWmsCalculations _referralCalcs;
        private readonly INhsLoginService _nhsLoginService;
        private readonly IConfiguration _config;
        private readonly IReferralSessionData _referralSessionData;

        public SelfReferralController(ILogger<SelfReferralController> logger, IWmsReferralService wmsSelfReferralService, IPostcodesioService postcodesioService,
            TelemetryClient telemetry, IWmsCalculations referralBusiness, INhsLoginService nhsLoginService, IGetAddressioService getAddressioService, IConfiguration configuration,
            IReferralSessionData referralSessionData)
        {
            _logger = logger;
            _WmsSelfReferralService = wmsSelfReferralService;
            _PostcodesioService = postcodesioService;
            _telemetry = telemetry;
            _referralCalcs = referralBusiness;
            _nhsLoginService = nhsLoginService;
            _GetAddressioService = getAddressioService;
            _config = configuration;
            _referralSessionData = referralSessionData;
        }
        public IActionResult GoneWrong()
        {
            _logger.LogInformation("Gone Wrong View");
            return View();
        }

        [Route("{controller}")]
        [Route("{controller}/login")]
        [Route("{controller}/register")]
        public async Task<IActionResult> Index()
        {
            string linkId = TempData[ControllerConstants.TEMPDATA_LINK_ID] as string;

            if (string.IsNullOrWhiteSpace(linkId))
            {
              return View("GoneWrong", GetErrorModel("User link ID is missing from URL."));
            }
            else
            {
              bool linkIdIsValid = ValidationHelper.LinkIdIsValid(linkId);

              if (!linkIdIsValid)
              {
                return View("GoneWrong", GetErrorModel("User link ID is not a valid link ID."));
              }

              bool linkIdIsMatch = await _WmsSelfReferralService.ValidateLinkId(linkId);
            
              if (!linkIdIsMatch)
              {
                return View("GoneWrong", GetErrorModel("Unable to match user link ID to referral."));
              }
            }

            var nhsLogin = await PopulateReferral();
            if (nhsLogin == null)
                return View("GoneWrong", GetErrorModel("User timed out for inactivity", HttpContext.TraceIdentifier));


            var referral = _referralSessionData.GetReferralSessionData();
            var keyanswers = _referralSessionData.GetAnswerSessionData();
            if (referral.NhsNumber != null)
            {
                //check nhsnumber against api
                var response = await _WmsSelfReferralService.NhsNumberCheckAsync(referral.NhsNumber);

                switch (response.StatusCode)
                {
                    case 200:
                        //use api referral 
                        if (keyanswers.QueriedReferral == false) //only use referral data once
                            _referralSessionData.SetReferralSessionData(response.Referral);
                        await PopulateReferral(true);
                        if (response.Referral.ReferralSource == "ElectiveCare")
                            return View("electivecare", nhsLogin);

                        return View("updatereferral", nhsLogin);
                    case 204:
                        return View(nhsLogin);
                    //return RedirectToAction("height");
                    case 400:
                        return View("GoneWrong", GetErrorModel("NHS number not valid"));
                    case 401:
                        return View("GoneWrong", GetErrorModel("An Error has occurred"));
                    case 409:
                        CultureInfo culture;
                        culture = CultureInfo.CreateSpecificCulture("en-GB");
                        var dateExists = DateTime.TryParse(response.Errors.GetValueOrDefault("dateOfReferral"), culture, DateTimeStyles.None, out DateTime DateOfRefferal);
                        return View("NotEligibleReferralExists", new ReferralExistsViewModel()
                        {
                            ErrorDescription = response.Errors.GetValueOrDefault("errorDescription"),
                            ChosenProvider = response.Errors.GetValueOrDefault("providerName") ?? "No provider chosen",
                            DateOfReferral = dateExists ? DateOfRefferal:null,
                            Name = referral.GivenName + " " + referral.FamilyName
                        });
                    case 500:
                        return View("GoneWrong", GetErrorModel("An Error has occurred"));
                    default:
                        return View("GoneWrong", GetErrorModel("An Error has occurred"));
                }

            }

            //return
            _logger.LogError("GoneWrong - No NHS number");
            return View("GoneWrong", GetErrorModel("An Error has occurred"));


        }


        [Route("{controller}/update-referral")]
        public async Task<IActionResult> UpdateReferral()
        {
            var nhsLogin = await PopulateReferral(true);

            return View(nhsLogin);
        }

        [Route("{controller}/sign-out")]
        public async Task<IActionResult> Signout()
        {
            //user wants to logout
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            //logout
            await HttpContext.SignOutAsync();

            //return
            return Redirect("~/");
        }

        [Route("{controller}/height")]
        public IActionResult Height()
        {
            var referral = _referralSessionData.GetReferralSessionData();
            //decimal? heightCm = referral.HeightCm != null ? Math.Round(referral.HeightCm.Value, 2) : null;

            //check age 
            var age = _referralCalcs.CalcAge(referral);
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

            if (referral.ConsentForGpAndNhsNumberLookup == null)
            { //first time, start referral
                referral.DateOfReferral = DateTime.UtcNow;
                referral.ConsentForGpAndNhsNumberLookup = true;
                _referralSessionData.SetReferralSessionData(referral);

                //mark answered
                var keyAnswers = _referralSessionData.GetAnswerSessionData();
                keyAnswers.AnsweredNhsNumberGPConsent = true;
                _referralSessionData.SetAnswerSessionData(keyAnswers);

            }

            return View(new HeightViewModel { Height = referral.HeightCm });
        }
        [HttpPost]
        [Route("{controller}/height")]
        public async Task<IActionResult> Height(HeightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

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

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HeightCm = model.Height;
            selfReferral.HeightUnits = "Metric";
            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("weight");
        }
        [Route("{controller}/weight")]
        public IActionResult Weight()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
            //decimal? weightKg = selfReferral.WeightKg!=null ? Math.Round(selfReferral.WeightKg.Value, 2) : null;
            
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
        public async Task<IActionResult> Weight(WeightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));


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

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.WeightUnits = "Metric";
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

            selfReferral.IsDateOfBmiAtRegistrationValid = true;
            _referralSessionData.SetReferralSessionData(selfReferral);


            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }
        [Route("{controller}/height-imperial")]
        public IActionResult HeightImperial()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
            
            var model = _referralCalcs.ConvertCm(selfReferral.HeightCm);
            //model.HeightIn = model.HeightIn != null ? Math.Round(model.HeightIn.Value, 2) : null;
            
            return View(model);
        }
        [HttpPost]
        [Route("{controller}/height-imperial")]
        public async Task<IActionResult> HeightImperial(HeightImperialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();            
            var heightCm = _referralCalcs.ConvertFeetInches(model.HeightFt, model.HeightIn);
            
            if (heightCm == null)
            {
                ModelState.AddModelError("HeightError", "Height values not valid");
                return View(model);
            }
            if (heightCm < 50)
            {
                ModelState.AddModelError("HeightError", "Height must be taller than 1ft 7.6in");
                return View(model);
            }
            if (heightCm > 250)
            {
                ModelState.AddModelError("HeightError", "Height must be lower than 8ft 2.4in");
                return View(model);
            }

            selfReferral.HeightCm = heightCm;
            selfReferral.HeightUnits = "Imperial";
            selfReferral.HeightFeet = model.HeightFt;
            selfReferral.HeightInches = model.HeightIn;
            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("weight-imperial");
        }

        [Route("{controller}/weight-imperial")]
        public IActionResult WeightImperial()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
            var wivm = _referralCalcs.ConvertKg(selfReferral.WeightKg);

            //decimal? weightLb = wivm.WeightLb != null ? Math.Round(wivm.WeightLb.Value, 1) : null;

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
        public async Task<IActionResult> WeightImperial(WeightImperialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();

            var weight = _referralCalcs.ConvertStonesPounds(model.WeightSt, model.WeightLb);
            if (weight == null)
            {
                ModelState.AddModelError("WeightError", "Weight values entered are not valid.");
                return View(model);
            }
            if (weight > 500)
            {
                ModelState.AddModelError("WeightError", "Weight must be lower than or equal to 78st and 10.3lbs");
                return View(model);
            }
            if (weight < 35)
            {
                ModelState.AddModelError("WeightError", "Weight must be greater than or equal to 5st and 7.1lbs");
                return View(model);
            }

            selfReferral.WeightKg = weight;
            selfReferral.WeightUnits = "Imperial";
            selfReferral.WeightStones = model.WeightSt;
            selfReferral.WeightPounds = model.WeightLb;

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

            selfReferral.IsDateOfBmiAtRegistrationValid = true;
            _referralSessionData.SetReferralSessionData(selfReferral);


            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }


        [Route("{controller}/select-ethnicity-group")]
        public async Task<IActionResult> EthnicityGroup()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
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

            return View(new EthnicityViewModel { EthnicityGroupList = ethnicities, ReferralEthnicityGroup = groupName, WeightUnits = selfReferral.WeightUnits });
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

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();

            if (model.ReferralEthnicityGroup.ToLower() == "i do not wish to disclose my ethnicity")
            {
                selfReferral.Ethnicity = "Other"; //triagename
                selfReferral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup; //groupname
                selfReferral.ServiceUserEthnicity = model.ReferralEthnicityGroup; //displayname
                _referralSessionData.SetReferralSessionData(selfReferral);

                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

                return RedirectToAction("date-of-birth");
            }

            selfReferral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup;
            _referralSessionData.SetReferralSessionData(selfReferral);

            return RedirectToAction("Select-Ethnicity", new { id = model.ReferralEthnicityGroup });
        }
        [Route("{controller}/select-ethnicity/")]
        [Route("{controller}/select-ethnicity/{id}")]
        public async Task<IActionResult> Ethnicity(string id)
        {

            var selfReferral = _referralSessionData.GetReferralSessionData();
            if (id == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:No Ethnicity");
                return View("GoneWrong", GetErrorModel("Something went wrong"));
            }

            if (id.ToLower() == "i do not wish to disclose my ethnicity")
                return RedirectToAction("select-ethnicity-group");

            return View(new EthnicityViewModel
            {
                EthnicityGroupList = await _WmsSelfReferralService.GetEthnicityGroupList(APIENDPOINT),
                EthnicityGroupDescription = id,
                EthnicityList = await _WmsSelfReferralService.GetEthnicityMembersList(id, APIENDPOINT),
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
                    EthnicityList = await _WmsSelfReferralService.GetEthnicityMembersList(model.ReferralEthnicityGroup, APIENDPOINT),
                    ReferralEthnicityGroup = model.ReferralEthnicityGroup,
                    SelectedEthnicity = model.ReferralEthnicity
                });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var ethnicities = await _WmsSelfReferralService.GetEthnicities(APIENDPOINT);
            if (ethnicities == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", selfReferral.Email }, { "Error", "Ethnicity API null" } });
                return View("GoneWrong", GetErrorModel("Service error"));
            }

            selfReferral.Ethnicity = ethnicities.ToList().Where(w => w.DisplayName == model.ReferralEthnicity).First().TriageName;
            selfReferral.ServiceUserEthnicity = model.ReferralEthnicity; //displayname
            _referralSessionData.SetReferralSessionData(selfReferral);

            //bmi check
            var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
            if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
            if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("date-of-birth");
        }

        [Route("{controller}/date-of-birth")]
        public async Task<IActionResult> DateofBirth()
        {

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();

            if (selfReferral.DateOfBirth == null)
                return View(new DateOfBirthViewModel { BackActionRoute = selfReferral.ServiceUserEthnicityGroup });

            var keyanswers = _referralSessionData.GetAnswerSessionData();

            return View(new DateOfBirthViewModel
            {
                Day = selfReferral.DateOfBirth.Value.Day,
                Month = selfReferral.DateOfBirth.Value.Month,
                Year = selfReferral.DateOfBirth.Value.Year,
                BackActionRoute = selfReferral.ServiceUserEthnicityGroup

            });
        }
        [HttpPost]
        [Route("{controller}/date-of-birth")]
        public async Task<IActionResult> DateofBirth(DateOfBirthViewModel model)
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();

            if (!ModelState.IsValid)
            {
                return View(new DateOfBirthViewModel
                {
                    Day = model.Day,
                    Month = model.Month,
                    Year = model.Year,
                    BackActionRoute = selfReferral.ServiceUserEthnicityGroup
                });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));


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
            //_referralSessionData.SetReferralSessionData(selfReferral); don't allow dob to be set - nhs login

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("sex-at-birth");
        }
        [Route("{controller}/sex-at-birth")]
        public async Task<IActionResult> Sex()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new SexViewModel { Sex = selfReferral.Sex, Sexes = StaticReferralHelper.GetSexes() });
        }
        [HttpPost]
        [Route("{controller}/sex-at-birth")]
        public async Task<IActionResult> Sex(SexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new SexViewModel { Sex = model.Sex, Sexes = StaticReferralHelper.GetSexes() });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.Sex = model.Sex;

            //if male, make sure pregnant is null
            if (model.Sex == "Male")
            {
                selfReferral.IsPregnant = null;
            }

            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("bariatric-surgery");
        }
        [Route("{controller}/bariatric-surgery")]
        public async Task<IActionResult> BariatricSurgery()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            return View(new BariatricSurgeryViewModel { BariatricSurgery = keyAnswers.AnsweredBariatricSurgery ? selfReferral.HasHadBariatricSurgery == null ? "null" : selfReferral.HasHadBariatricSurgery == true ? "true" : "false" : "", YNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null") });
        }
        [HttpPost]
        [Route("{controller}/bariatric-surgery")]
        public async Task<IActionResult> BariatricSurgery(BariatricSurgeryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new BariatricSurgeryViewModel { BariatricSurgery = model.BariatricSurgery, YNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null") });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HasHadBariatricSurgery = model.BariatricSurgery == "true" ? true : model.BariatricSurgery == "false" ? false : null;


            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredBariatricSurgery = true;


            if (selfReferral.HasHadBariatricSurgery == true)
            {
                //selfReferral.HasHadBariatricSurgery = null;
                _referralSessionData.SetReferralSessionData(selfReferral);
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BariatricSurgery" });
            }

            _referralSessionData.SetReferralSessionData(selfReferral);
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");


            return RedirectToAction("active-eating-disorder");

        }
        [Route("{controller}/active-eating-disorder")]
        public async Task<IActionResult> EatingDisorder()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            return View(new EatingDisorderViewModel { EatingDisorder = keyAnswers.AnsweredEatingDisorder ? selfReferral.HasActiveEatingDisorder == null ? "null" : selfReferral.HasActiveEatingDisorder == true ? "true" : "false" : "", YNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null") });
        }
        [HttpPost]
        [Route("{controller}/active-eating-disorder")]
        public async Task<IActionResult> EatingDisorder(EatingDisorderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new EatingDisorderViewModel { EatingDisorder = model.EatingDisorder, YNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null") });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HasActiveEatingDisorder = model.EatingDisorder == "true" ? true : model.EatingDisorder == "false" ? false : null;


            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredEatingDisorder = true;


            if (selfReferral.HasActiveEatingDisorder == true)
            {
                //selfReferral.HasActiveEatingDisorder = null;
                _referralSessionData.SetReferralSessionData(selfReferral);
                return RedirectToAction("Not-Eligible-For-Service", new { id = "ActiveEatingDisorder" });
            }

            _referralSessionData.SetReferralSessionData(selfReferral);
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            if (selfReferral.Sex == "Female")
                return RedirectToAction("are-you-pregnant");

            return RedirectToAction("family-name");
        }
        [Route("{controller}/are-you-pregnant")]
        public async Task<IActionResult> Pregnant()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();

            var ynList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null").ToList();
            //ynList.Add(new KeyValuePair<string, string>("null", "Not applicable / Prefer not to say"));

            return View(new PregnantViewModel { Pregnant = keyAnswers.AnsweredPregnant ? selfReferral.IsPregnant == null ? "null" : selfReferral.IsPregnant == true ? "true" : "false" : "", YNList = ynList });
        }
        [HttpPost]
        [Route("{controller}/are-you-pregnant")]
        public async Task<IActionResult> Pregnant(PregnantViewModel model)
        {
            var ynList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null").ToList();
            //ynList.Add(new KeyValuePair<string, string>("null", "Not applicable / Prefer not to say"));

            if (!ModelState.IsValid)
            {
                return View(new PregnantViewModel { Pregnant = model.Pregnant, YNList = ynList });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.IsPregnant = model.Pregnant == "true" ? true : model.Pregnant == "false" ? false : null;
            _referralSessionData.SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredPregnant = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (selfReferral.IsPregnant == true)
            {
                return RedirectToAction("Not-Eligible-For-Service", new { id = "ArePregnant" });
            }

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("family-name");
        }




        [Route("{controller}/family-name")]
        public async Task<IActionResult> FamilyName()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new FamilyNameViewModel { FamilyName = selfReferral.FamilyName, BackActionRoute = selfReferral.Sex == "Male" ? "active-eating-disorder" : "are-you-pregnant" });
        }
        [HttpPost]
        [Route("{controller}/family-name")]
        public async Task<IActionResult> FamilyName(FamilyNameViewModel model)
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            if (!ModelState.IsValid)
            {
                return View(new FamilyNameViewModel { FamilyName = model.FamilyName, BackActionRoute = selfReferral.ServiceUserEthnicityGroup });
            }

            selfReferral.FamilyName = StaticReferralHelper.StringCleaner(model.FamilyName);

            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("given-name");
        }
        [Route("{controller}/given-name")]
        public async Task<IActionResult> GivenName()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new GivenNameViewModel { GivenName = selfReferral.GivenName });
        }
        [HttpPost]
        [Route("{controller}/given-name")]
        public async Task<IActionResult> GivenName(GivenNameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.GivenName = StaticReferralHelper.StringCleaner(model.GivenName);
            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("find-address");
        }
        [Route("{controller}/find-address")]
        public async Task<IActionResult> FindAddress()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new FindAddressViewModel
            {
                Postcode = selfReferral.Postcode
            });
        }
        [HttpPost]
        [Route("{controller}/find-address")]
        public async Task<IActionResult> FindAddress(FindAddressViewModel model)
        {
            //var selfReferral = _referralSessionData.GetReferralSessionData();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                //validate postcode
                var addresses = await _GetAddressioService.GetAddressList(model.Postcode);
                if (addresses.First().Key == "Error")
                {
                    //error occured
                    //warn user
                    model.PostCodeValid = false;
                    switch (addresses.First().Value)
                    {
                        case "404":
                            ModelState.AddModelError("Postcode", "The postcode entered could not be found.");
                            break;
                        case "400":
                            ModelState.AddModelError("Postcode", "The postcode entered is not valid.");
                            break;
                        default:
                            ModelState.AddModelError("Postcode", "The postcode entered could not be found.");
                            break;
                    }

                    return View("FindAddress", model);
                }
                model.AddressList = addresses;
                return View("FindAddressConfirm", model);

            }
            catch (Exception ex)
            {
                _telemetry.TrackEvent("Error: GetAddressIO");
                _telemetry.TrackException(ex);
                _logger.LogError(ex.Message);
            }
            return View("FindAddress", model);
        }
        [HttpPost]
        [Route("{controller}/find-address-confirm")]
        public IActionResult FindAddressConfirm(FindAddressViewModel model)
        {

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var selectedAddress = model.Address.Split(",");

            selfReferral.Address1 = selectedAddress[0].Trim();
            selfReferral.Address2 = selectedAddress[5].Trim();
            selfReferral.Address3 = selectedAddress[6].Trim();
            selfReferral.Postcode = model.Postcode.ToUpper().Trim();
            _referralSessionData.SetReferralSessionData(selfReferral);

            return View("Address", new AddressViewModelV1()
            {
                Address1 = selfReferral.Address1,
                Address2 = selfReferral.Address2,
                Address3 = selfReferral.Address3,
                Postcode = selfReferral.Postcode,
                PostCodeValid = true
            });
        }

        [Route("{controller}/address")]
        public IActionResult Address()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
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

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            if (model.Postcode != "" && selfReferral.Postcode?.ToUpper() != model.Postcode.ToUpper())
            {
                //user entered different postcode - revalidate
                model.UserWarned = false;
            }

            selfReferral.Address1 = StaticReferralHelper.StringCleaner(model.Address1);
            selfReferral.Address2 = StaticReferralHelper.StringCleaner(model.Address2);
            selfReferral.Address3 = StaticReferralHelper.StringCleaner(model.Address3);
            selfReferral.Postcode = StaticReferralHelper.StringCleaner(model.Postcode.ToUpper());
            model.Postcode = model.Postcode.ToUpper();
            _referralSessionData.SetReferralSessionData(selfReferral);

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

                //check allowed countries
                string allowedCountries = _config?.GetValue<string>("WmsSelfReferral:AllowedCountries") ?? "any";                
                if (allowedCountries != "any")
                {
                    var pcLookup = await _PostcodesioService.LookupPostCodeAsync(model.Postcode);
                    if (pcLookup.Status == 200)
                    {
                        //if returned pc country is not in list then reject                        
                        if (!allowedCountries.ToLower().Contains(pcLookup.Result.Country.ToLower()))
                        {
                            return RedirectToAction("Not-Eligible-For-Service", new { id = "AllowedCountry" });
                        }
                       
                    }
                }

            }
            catch (Exception ex)
            {
                _telemetry.TrackEvent("Error: PostCodeIO");
                _telemetry.TrackException(ex);
                _logger.LogError(ex.Message);
            }


            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("email-address");
        }
        [Route("{controller}/email-address")]
        public async Task<IActionResult> EmailAddress()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new EmailAddressViewModel { Email = selfReferral.Email });
        }
        [HttpPost]
        [Route("{controller}/email-address")]
        public IActionResult EmailAddress(EmailAddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new EmailAddressViewModel { Email = model.Email });
            }

            //simple domain check
            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAILADDRESS);
            if (!validDomain.IsMatch(model.Email))
            {
                ModelState.AddModelError("Email", "Email address not valid");
                return View(new EmailAddressViewModel { Email = model.Email });
            }


            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.Email = model.Email;
            _referralSessionData.SetReferralSessionData(selfReferral);


            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("mobile");
        }
        [Route("{controller}/mobile")]
        public async Task<IActionResult> Mobile()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new MobileViewModel { Mobile = selfReferral.Mobile });
        }
        [HttpPost]
        [Route("{controller}/mobile")]
        public async Task<IActionResult> Mobile(MobileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.Mobile = model.Mobile;
            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("telephone");
        }
        [Route("{controller}/telephone")]
        public async Task<IActionResult> Telephone()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            return View(new TelephoneViewModel { Telephone = selfReferral.Telephone });
        }
        [HttpPost]
        [Route("{controller}/telephone")]
        public async Task<IActionResult> Telephone(TelephoneViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.Telephone = model.Telephone;
            _referralSessionData.SetReferralSessionData(selfReferral);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("consent-for-future-contact");
        }
        [Route("{controller}/consent-for-future-contact")]
        public async Task<IActionResult> ConsentFutureContact()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
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
        public async Task<IActionResult> ConsentFutureContact(ConsentFutureContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new ConsentFutureContactViewModel
                {
                    FutureContact = model.FutureContact,
                    FutureContactList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null").ToList()
                });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.ConsentForFutureContactForEvaluation = model.FutureContact == "true";
            _referralSessionData.SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredConsentForFurtureContact = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("consent-to-referrer-update");
        }

        [Route("{controller}/consent-to-referrer-update")]
        public IActionResult ConsentReferrerUpdate()
        {
            var referral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            return View(new ConsentForReferrerUpdateViewModel
            {
                ConsentToReferrerUpdate = keyAnswers.AnsweredUpdateReferrerCompletionConsent ? referral.ConsentForReferrerUpdatedWithOutcome == true ? "true" : "false" : "",
                ConsentYNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
            });
        }
        [HttpPost]
        [Route("{controller}/consent-to-referrer-update")]
        public IActionResult ConsentReferrerUpdate(ConsentForReferrerUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new ConsentForReferrerUpdateViewModel
                {
                    ConsentToReferrerUpdate = model.ConsentToReferrerUpdate,
                    ConsentYNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            var referral = _referralSessionData.GetReferralSessionData();

            referral.ConsentForReferrerUpdatedWithOutcome = model.ConsentToReferrerUpdate == "true";
            _referralSessionData.SetReferralSessionData(referral);


            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredUpdateReferrerCompletionConsent = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("medical-conditions");
        }


        [Route("{controller}/medical-conditions")]
        public async Task<IActionResult> MedicalConditions()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            var ynList = StaticReferralHelper.GetYNList().ToList();
            //HasVulnerabilities = keyAnswers.AnsweredPatientVulnerable ? referral.IsVulnerable == null ? "null" : referral.IsVulnerable == true ? "true" : "false" : ""
            return View(new MedicalConditionsViewModel
            {
                TypeOneDiabetes = keyAnswers.AnsweredDiabetesType1 ? selfReferral.HasDiabetesType1 == null ? "null" : selfReferral.HasDiabetesType1 == true ? "true" : "false" : "",
                TypeTwoDiabetes = keyAnswers.AnsweredDiabetesType2 ? selfReferral.HasDiabetesType2 == null ? "null" : selfReferral.HasDiabetesType2 == true ? "true" : "false" : "",
                ArthritisHip = keyAnswers.AnsweredArthritisHip ? selfReferral.HasArthritisOfHip == null ? "null" : selfReferral.HasArthritisOfHip == true ? "true" : "false" : "",
                ArthritisKnee = keyAnswers.AnsweredArthritisKnee ? selfReferral.HasArthritisOfKnee == null ? "null" : selfReferral.HasArthritisOfKnee == true ? "true" : "false" : "",
                Hypertension = keyAnswers.AnsweredHypertension ? selfReferral.HasHypertension == null ? "null" : selfReferral.HasHypertension == true ? "true" : "false" : "",
                YNList = ynList
            });
        }
        [HttpPost]
        [Route("{controller}/medical-conditions")]
        public async Task<IActionResult> MedicalConditions(MedicalConditionsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var ynList = StaticReferralHelper.GetYNList().ToList();
                model.YNList = ynList;
                return View(model);
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HasDiabetesType1 = model.TypeOneDiabetes == "true" ? true : model.TypeOneDiabetes == "false" ? false : null;
            selfReferral.HasDiabetesType2 = model.TypeTwoDiabetes == "true" ? true : model.TypeTwoDiabetes == "false" ? false : null;
            selfReferral.HasHypertension = model.Hypertension == "true" ? true : model.Hypertension == "false" ? false : null;
            selfReferral.HasArthritisOfHip = model.ArthritisHip == "true" ? true : model.ArthritisHip == "false" ? false : null;
            selfReferral.HasArthritisOfKnee = model.ArthritisKnee == "true" ? true : model.ArthritisKnee == "false" ? false : null;
            _referralSessionData.SetReferralSessionData(selfReferral);

            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType1 = true;
            keyAnswers.AnsweredDiabetesType2 = true;
            keyAnswers.AnsweredHypertension = true;
            keyAnswers.AnsweredArthritisHip = true;
            keyAnswers.AnsweredArthritisKnee = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);



            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("physical-disability");
        }

        [Route("{controller}/physical-disability")]
        public async Task<IActionResult> PhysicalDisability()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            return View(new PhysicalDisabilityViewModel
            {
                PhysicalDisability = keyAnswers.AnsweredPhysicalDisability ? selfReferral.HasAPhysicalDisability == null ? "null" : selfReferral.HasAPhysicalDisability == true ? "true" : "false" : "",
                PhysicalDisabilityList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/physical-disability")]
        public async Task<IActionResult> PhysicalDisability(PhysicalDisabilityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new PhysicalDisabilityViewModel
                {
                    PhysicalDisability = model.PhysicalDisability,
                    PhysicalDisabilityList = StaticReferralHelper.GetYNList()
                });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HasAPhysicalDisability = model.PhysicalDisability == "true" ? true : model.PhysicalDisability == "false" ? false : null;
            _referralSessionData.SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredPhysicalDisability = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Learning-Disability");
        }
        [Route("{controller}/learning-disability")]
        public async Task<IActionResult> LearningDisability()
        {
            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            return View(new LearningDisabilityViewModel
            {
                LearningDisability = keyAnswers.AnsweredLearningDisability ? selfReferral.HasALearningDisability == null ? "null" : selfReferral.HasALearningDisability == true ? "true" : "false" : "",
                LearningDisabilityList = StaticReferralHelper.GetYNList()
            });
        }
        [HttpPost]
        [Route("{controller}/learning-disability")]
        public async Task<IActionResult> LearningDisability(LearningDisabilityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new LearningDisabilityViewModel
                {
                    LearningDisability = model.LearningDisability,
                    LearningDisabilityList = StaticReferralHelper.GetYNList()
                });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            var selfReferral = _referralSessionData.GetReferralSessionData();
            selfReferral.HasALearningDisability = model.LearningDisability == "true" ? true : model.LearningDisability == "false" ? false : null;
            _referralSessionData.SetReferralSessionData(selfReferral);

            //mark answered
            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredLearningDisability = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (_referralSessionData.ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("check-answers");
        }

        [Route("{controller}/check-answers")]
        public async Task<IActionResult> CheckAnswers()
        {
            var selfReferral = _referralSessionData.GetReferralSessionData();
            var keyAnswers = _referralSessionData.GetAnswerSessionData();

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

            if (selfReferral.HasHadBariatricSurgery == true)
            {
                selfReferral.HasHadBariatricSurgery = null;
                keyAnswers.AnsweredBariatricSurgery = false;
                _referralSessionData.SetAnswerSessionData(keyAnswers);
                _referralSessionData.SetReferralSessionData(selfReferral);
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BariatricSurgery" });
            }
            if (selfReferral.HasActiveEatingDisorder == true)
            {
                selfReferral.HasActiveEatingDisorder = null;
                keyAnswers.AnsweredEatingDisorder = false;
                _referralSessionData.SetAnswerSessionData(keyAnswers);
                _referralSessionData.SetReferralSessionData(selfReferral);
                return RedirectToAction("Not-Eligible-For-Service", new { id = "ActiveEatingDisorder" });
            }
            if (selfReferral.IsPregnant == true)
            {
                selfReferral.IsPregnant = null;
                keyAnswers.AnsweredPregnant = false;
                _referralSessionData.SetAnswerSessionData(keyAnswers);
                _referralSessionData.SetReferralSessionData(selfReferral);
                return RedirectToAction("Not-Eligible-For-Service", new { id = "ArePregnant" });
            }

            if (await _referralSessionData.NotValidSession())//error, redirect           
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));

            //set imperial            
            keyAnswers.HeightImperial = new HeightImperialViewModel() 
            { 
                HeightFt = (int?)selfReferral.HeightFeet, 
                HeightIn = selfReferral.HeightInches 
            } ?? keyAnswers.HeightImperial;

            keyAnswers.WeightImperial = new WeightImperialViewModel() 
            { 
                WeightSt = (int?)selfReferral.WeightStones,
                WeightLb = selfReferral.WeightPounds
            } ?? keyAnswers.WeightImperial;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            if (keyAnswers.HeightImperial == null || keyAnswers.WeightImperial == null)
            {
                //imperial units are null, likely a timeout or user has completed and is retrying
                _telemetry.TrackEvent("GoneWrong:Not complete");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted or is incomplete"));
            }

            if (!_referralSessionData.ReferralCompleted())
            {
                _telemetry.TrackEvent("GoneWrong:Not complete");
                return View("GoneWrong", GetErrorModel("Referral is incomplete, please try again"));
            }


            return View(new CheckAnswersViewModel { Referral = selfReferral, KeyAnswer = keyAnswers, NhsLogin = await PopulateReferral() });
        }
        [HttpPost]
        [Route("{controller}/check-answers")]
        public async Task<IActionResult> CheckAnswers(CheckAnswersViewModel model)
        {
            try
            {
                var selfReferral = _referralSessionData.GetReferralSessionData();
                var keyAnswers = _referralSessionData.GetAnswerSessionData();

                if (selfReferral.HasHadBariatricSurgery != false)
                    return RedirectToAction("check-answers");
                if (selfReferral.HasActiveEatingDisorder != false)
                    return RedirectToAction("check-answers");
                if (selfReferral.IsPregnant == true)
                    return RedirectToAction("check-answers");
                if (selfReferral.IsPregnant == null
                    && selfReferral.Sex == "Female"
                    && keyAnswers.AnsweredPregnant == false)
                    return RedirectToAction("check-answers");


                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(selfReferral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });


                //set last answered fields
                selfReferral.LastSubmitEmail = selfReferral.Email;
                selfReferral.LastSubmitFamilyName = selfReferral.FamilyName;
                selfReferral.LastSubmitGivenName = selfReferral.GivenName;
                selfReferral.LastSubmitMobile = selfReferral.Mobile;
                _referralSessionData.SetReferralSessionData(selfReferral);

                //format phone numbers
                selfReferral = (SelfReferral)StaticReferralHelper.FinalCheckAnswerChecks(selfReferral);

                //post to api
                var result = selfReferral.Id == null ?
                    await _WmsSelfReferralService.AddSelfReferralAsync(selfReferral) :
                    await _WmsSelfReferralService.UpdateSelfReferralAsync(selfReferral);

                //if success
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    var referral = JsonConvert.DeserializeObject<ProviderChoiceModel>(await result.Content.ReadAsStringAsync());
                    //add additional choice
                    referral.ProviderChoices.Add(new Provider()
                    {
                        Id = new Guid("2021c46b-ce2a-4e0d-8dd9-c38af0813ade"),
                        Name = "Need more time to decide?",
                        Summary = "If you need more time to choose a service, select this option and we'll send you a text message within 2 working days. This message, from ‘NHS Service’, will contain a link back to this website from which you can access the list of available programmes. "

                    });
                    _referralSessionData.SetProviderChoiceSessionData(referral);


                    keyAnswers.ReferralSubmitted = true;
                    _referralSessionData.SetAnswerSessionData(keyAnswers);

                    return RedirectToAction("provider-choice");
                }

                //if no providers available
                if (result.StatusCode == HttpStatusCode.NoContent)
                {
                    //redirect to no providers page
                    return View("NoProviders");
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
            if (_referralSessionData.ReferralSubmitted())
            {
                //error, already submitted
                _telemetry.TrackEvent("GoneWrong:Already submitted");
                return View("GoneWrong", GetErrorModel("Referral has already been submitted"));
            }

            var providerChoice = _referralSessionData.GetProviderChoiceSessionData();
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
            var providerChoice = _referralSessionData.GetProviderChoiceSessionData();
            model.ProviderChoices = providerChoice.ProviderChoices;
            model.Id = providerChoice.Id;

            if (_referralSessionData.ReferralSubmitted())
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
            _referralSessionData.SetProviderChoiceSessionData(providerChoice);


            return View("providerconfirm", providerChoice);
        }

        [HttpPost]
        [Route("{controller}/provider-confirm")]
        public async Task<IActionResult> ProviderConfirm(ProviderChoiceModel model)
        {
            try
            {
                var providerChoice = _referralSessionData.GetProviderChoiceSessionData();
                model.ProviderId = model.Provider.Id;
                model.ProviderChoices = providerChoice.ProviderChoices;
                model.Id = providerChoice.Id;

                if (_referralSessionData.ReferralSubmitted())
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
                var result = await _WmsSelfReferralService.UpdateProviderChoiceAsync(model, model.Id.ToString());

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
            //HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            //HttpContext.Session.Remove(SESSION_PROVIDER_CHOICE_TOKEN);

            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType1 = false;
            keyAnswers.AnsweredDiabetesType2 = false;
            keyAnswers.AnsweredLearningDisability = false;
            keyAnswers.AnsweredHypertension = false;
            keyAnswers.AnsweredPhysicalDisability = false;
            //mark providerchoice submitted
            keyAnswers.ProviderChoiceSubmitted = true;
            _referralSessionData.SetAnswerSessionData(keyAnswers);


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

            var referral = _referralSessionData.GetReferralSessionData();
            _telemetry.TrackEvent("NotEligible", new Dictionary<string, string> {
                { "Age",_referralCalcs.CalcAge(referral).ToString() ?? "NotSet" },
                { "BMI",_referralCalcs.CalcBmi(referral).ToString() ?? "NotSet" },
                { "PostCode", referral.Postcode ?? "NotSet" },
                { "Reason", id }
            });


            //remove sessions            
            //HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);
            //HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            var referralSource = referral.ReferralSource?.ToLower() ?? "";
            if (referralSource == "electivecare")
            {
                //not elgible and source is ElectiveCare
                return id switch
                {
                    "BMI" => View("NotEligibleBMI-ECR", new NotEligibleViewModel { Message = "", NonEligibiltyReason = "BMI" }),
                    "BMI-Limit" => View("NotEligibleBMILimit-ECR", new NotEligibleViewModel { Message = "", NonEligibiltyReason = "BMI-Limit" }),
                    "BariatricSurgery" => View("NotEligibleBariatric-ECR", new NotEligibleViewModel { Message = "", NonEligibiltyReason = "BariatricSurgery" }),
                    "ActiveEatingDisorder" => View("NotEligibleEatingDisorder-ECR", new NotEligibleViewModel { Message = "", NonEligibiltyReason = "ActiveEatingDisorder" }),
                    "ArePregnant" => View("NotEligibleArePregnant-ECR", new NotEligibleViewModel { Message = "", NonEligibiltyReason = "ArePregnant" }),
                    "AllowedCountry" => View("NotEligibleCountry", new NotEligibleViewModel { Message = "" }),
                    _ => View(new NotEligibleViewModel { Message = "" }),
                };

            }

            return id switch
            {
                "BMI" => View("NotEligibleBMI", new NotEligibleViewModel { Message = "" }),
                "BMI-Limit" => View("NotEligibleBMILimit", new NotEligibleViewModel { Message = "" }),
                "Email" => View("NotEligibleEmail", new NotEligibleViewModel { Message = "" }),
                "Age" => View("NotEligibleAge", new NotEligibleViewModel { Message = "" }),
                "Health" => View("NotEligibleHealth", new NotEligibleViewModel { Message = "" }),
                "NhsLookupConsent" => View("NotEligibleConsent", new NotEligibleViewModel { Message = "" }),
                "BariatricSurgery" => View("NotEligibleBariatric", new NotEligibleViewModel { Message = "" }),
                "ActiveEatingDisorder" => View("NotEligibleEatingDisorder", new NotEligibleViewModel { Message = "" }),
                "ArePregnant" => View("NotEligibleArePregnant", new NotEligibleViewModel { Message = "" }),
                "GivenBirth" => View("NotEligibleGivenBirth", new NotEligibleViewModel { Message = "" }),
                "BreastFeeding" => View("NotEligibleBreastFeeding", new NotEligibleViewModel { Message = "" }),
                "CaesareanSection" => View("NotEligibleCaesareanSection", new NotEligibleViewModel { Message = "" }),
                "AllowedCountry" => View("NotEligibleCountry", new NotEligibleViewModel { Message = "" }),
                _ => View(new NotEligibleViewModel { Message = "" }),
            };
        }

        [HttpPost]
        [Route("{controller}/Cancel-Electivecare")]
        public async Task<IActionResult> CancelElectiveCareReferral(NotEligibleViewModel model)
        {
            var referral = _referralSessionData.GetReferralSessionData();

            try
            {
                //call api to cancel referral
                var result = await _WmsSelfReferralService.CancelElectiveCareReferralAsync(referral);
                if (result.StatusCode == HttpStatusCode.OK)
                    return RedirectToAction("Cancelled", new { id = model.NonEligibiltyReason });

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
                        return View("GoneWrong", GetErrorModel("Referral not in valid state", telemErrors.GetValueOrDefault("TraceId")));
                    default:
                        //some other error, e.g. internal error
                        _telemetry.TrackEvent("GoneWrong", telemErrors);
                        return View("GoneWrong", GetErrorModel(result.StatusCode.ToString() + " An error has occured."));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error cancelling referral.");
                return View("GoneWrong", GetErrorModel(500 + " An error has occured."));
            }

        }

        [Route("{controller}/Cancelled")]
        public IActionResult Cancelled(string id)
        {
            _telemetry.TrackEvent("Elective Care Referral: Cancelled");

            var keyAnswers = _referralSessionData.GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType1 = false;
            keyAnswers.AnsweredDiabetesType2 = false;
            keyAnswers.AnsweredLearningDisability = false;
            keyAnswers.AnsweredHypertension = false;
            keyAnswers.AnsweredPhysicalDisability = false;
            keyAnswers.QueriedReferral = false;
            keyAnswers.ProviderChoiceSubmitted = false;
            _referralSessionData.SetAnswerSessionData(keyAnswers);

            //remove the self referral object
            HttpContext.Session.Remove(SESSION_KEY_SELFREFERRAL_TOKEN);

            return View();
        }


        [Route("{controller}/session-ping")]
        public IActionResult SessionPing()
        {
            return Ok();
        }
        private ErrorViewModel GetErrorModel(string message, string traceid = "")
        {
            return new ErrorViewModel()
            {
                RequestId = traceid,
                TraceId = traceid,
                Message = $"Error: {message}"
            };
        }
        private async Task<NhsLoginViewModel> PopulateReferral(bool updateReferral = false)
        {
            var logininfo = await GetNhsLogin();
            if (logininfo == null)
                return null;

            if (logininfo.HttpStatusCode == HttpStatusCode.OK)
            {
                var gpRegistration = logininfo.Claims.Where(w => w.Type.ToLower() == "gp_registration_details").FirstOrDefault()?.Value;
                if (gpRegistration == null)
                    gpRegistration = "{\"gp_ods_code\": \"V81999\"}";

                var gpOds = JsonConvert.DeserializeAnonymousType(gpRegistration, new { gp_ods_code = "" });

                var nhsLogin = new NhsLoginViewModel
                {
                    Email = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "email")
                        .FirstOrDefault()?.Value,
                    Email_verified = bool.TryParse(logininfo.Claims
                        .Where(w => w.Type.ToLower() == "email_verified")
                        .FirstOrDefault()?.Value, out bool verifiedEmail) ? verifiedEmail : null,
                    Phone = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "phone_number")
                        .FirstOrDefault()?.Value,
                    Phone_number_verified = bool.TryParse(logininfo.Claims
                        .Where(w => w.Type.ToLower() == "phone_number_verified")
                        .FirstOrDefault()?.Value, out bool verifiedPhone) ? verifiedPhone : null,
                    Birthdate = DateTime.TryParse(logininfo.Claims
                        .Where(w => w.Type.ToLower() == "birthdate")
                        .FirstOrDefault()?.Value, out DateTime trybirthdate) ? trybirthdate : null,
                    Family_name = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "family_name")
                        .FirstOrDefault()?.Value,
                    Given_name = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "given_name")
                        .FirstOrDefault()?.Value,
                    Nhs_number = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "nhs_number")
                        .FirstOrDefault()?.Value,
                    Gp_ods_code = gpOds?.gp_ods_code,
                    Gp_registration_details = new Gp_registration_details() { Gp_ods_code = gpOds?.gp_ods_code },
                    Identity_proofing_level = logininfo.Claims
                        .Where(w => w.Type.ToLower() == "identity_proofing_level")
                        .FirstOrDefault()?.Value
                };


                //build referral
                var selfReferral = _referralSessionData.GetReferralSessionData();
                selfReferral.Email ??= nhsLogin.Email;
                selfReferral.Mobile ??= nhsLogin.Phone;
                selfReferral.ReferringGpPracticeNumber ??= nhsLogin.Gp_ods_code;
                selfReferral.NhsNumber ??= nhsLogin.Nhs_number;
                selfReferral.DateOfBirth ??= nhsLogin.Birthdate;
                selfReferral.FamilyName ??= nhsLogin.Family_name;
                selfReferral.GivenName ??= nhsLogin.Given_name;
                selfReferral.DateOfReferral = DateTime.UtcNow;

                //set claims
                selfReferral.NhsLoginClaimEmail = nhsLogin.Email;
                selfReferral.NhsLoginClaimFamilyName = nhsLogin.Family_name;
                selfReferral.NhsLoginClaimGivenName = nhsLogin.Given_name;
                selfReferral.NhsLoginClaimMobile = nhsLogin.Phone;

                var keyanswers = _referralSessionData.GetAnswerSessionData();

                keyanswers.Identity_proofing_level = nhsLogin.Identity_proofing_level;
                keyanswers.QueriedReferral ??= false; //only set to false first run

                if (keyanswers.QueriedReferral == false)
                {
                    //when user comes back, ensure we retain previous answers for comparison
                    selfReferral.LastSubmitEmail = selfReferral.Email;
                    selfReferral.LastSubmitMobile = selfReferral.Mobile;
                    selfReferral.LastSubmitFamilyName = selfReferral.FamilyName;
                    selfReferral.LastSubmitGivenName = selfReferral.GivenName;
                }

                if (updateReferral)
                {
                    if (selfReferral.ReferralSource == "GeneralReferral")
                    {
                        keyanswers.AnsweredDiabetesType1 = true;
                        keyanswers.AnsweredDiabetesType2 = true;
                        keyanswers.AnsweredHypertension = true;
                        keyanswers.AnsweredArthritisHip = true;
                        keyanswers.AnsweredArthritisKnee = true;
                        keyanswers.AnsweredLearningDisability = true;
                        keyanswers.AnsweredPhysicalDisability = true;
                        keyanswers.AnsweredBariatricSurgery = true;
                        keyanswers.AnsweredBreastFeeding = true;
                        keyanswers.AnsweredCaesareanSection = true;
                        keyanswers.AnsweredEatingDisorder = true;
                        keyanswers.AnsweredGivenBirth = true;
                        if (selfReferral.IsPregnant==false)
                            keyanswers.AnsweredPregnant= true;
                        //keyanswers.AnsweredPregnant = true;
                        keyanswers.AnsweredNhsNumberGPConsent = true;
                        keyanswers.AnsweredUpdateReferrerCompletionConsent = true;
                        keyanswers.AnsweredConsentForFurtureContact = true;
                        keyanswers.ProviderChoiceSubmitted = false;
                    }
                    keyanswers.ReferralSubmitted = false;
                    keyanswers.QueriedReferral = true;
                }
                _referralSessionData.SetAnswerSessionData(keyanswers);




                if (updateReferral)
                {
                    //selfReferral.Email = nhsLogin.Email;
                    //selfReferral.Mobile = nhsLogin.Phone;
                    selfReferral.ReferringGpPracticeNumber = nhsLogin.Gp_ods_code;
                    selfReferral.NhsNumber = nhsLogin.Nhs_number;
                    selfReferral.DateOfBirth = nhsLogin.Birthdate;
                    //selfReferral.FamilyName = nhsLogin.Family_name;
                    //selfReferral.GivenName = nhsLogin.Given_name;
                    selfReferral.DateOfReferral = DateTime.UtcNow;
                    selfReferral.ConsentForGpAndNhsNumberLookup = true;
                }
                _referralSessionData.SetReferralSessionData(selfReferral);


                return nhsLogin;
            }

            return null;
        }

        private async Task<UserInfoResponse> GetNhsLogin()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var userinfo = await _nhsLoginService.GetUserInfo(accessToken);
            if (userinfo?.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("NHS Login: UserEndpoint User Unauthorised");
                await HttpContext.SignOutAsync();
            }

            if (userinfo?.HttpStatusCode == HttpStatusCode.NotFound) //profile deleted?
            {
                _logger.LogError("NHS Login: UserEndpoint User NotFound");
                await HttpContext.SignOutAsync();
            }

            return userinfo;
        }

        

    }
}
