using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsPharmacyReferral.Models;
using WmsPharmacyReferral.Helpers;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net;
using Microsoft.ApplicationInsights;
using WmsReferral.Business.Models;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Services;
using WmsReferral.Business;
using WmsReferral.Business.Shared;
using System.Net.Mail;

namespace WmsPharmacyReferral.Controllers
{
    public class PharmacyReferralController : SessionControllerBase
    {
        private const string SESSION_KEY_PHARMACYREFERRAL_TOKEN = "PharmacyReferral";
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";
        private const string APIENDPOINT = "PharmacyReferral";
        private readonly ILogger<PharmacyReferralController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWmsReferralService _WmsReferralService;
        private readonly IPostcodesioService _PostcodesioService;
        private readonly IODSLookupService _ODSLookupService;
        private TelemetryClient _telemetry;
        private readonly IWmsCalculations _referralCalcs;


        public PharmacyReferralController(ILogger<PharmacyReferralController> logger, IEmailSender emailSender, IWmsReferralService wmsReferralService, 
            IPostcodesioService postcodesioService, IODSLookupService odsLookupService, TelemetryClient telemetry, IWmsCalculations referralCalcs)
        {
            _logger = logger;
            _emailSender = emailSender;
            _WmsReferralService = wmsReferralService;
            _PostcodesioService = postcodesioService;
            _ODSLookupService = odsLookupService;
            _telemetry = telemetry;
            _referralCalcs = referralCalcs;
        }
        public IActionResult GoneWrong()
        {
            _logger.LogInformation("Gone Wrong View");
            return View();
        }

        [Route("{controller}")]
        public IActionResult Index()
        {
            var authSession = HttpContext.Session.Get<AuthViewModel>(SESSION_KEY_EMAIL);
            var pharmSession = HttpContext.Session.Get<PharmacyReferral>(SESSION_KEY_PHARMACYREFERRAL_TOKEN);

           

            if (authSession==null)//null
                return View("GoneWrong", GetErrorModel("Session lost."));

            if (authSession.IsAuthorised == false)//null
                return Redirect("/");




            return View(authSession);
        }
        [Route("{controller}/start-over")]
        public IActionResult StartOver()
        {
            var authSession = HttpContext.Session.Get<AuthViewModel>(SESSION_KEY_EMAIL);

            if (authSession == null)//null
                return View("GoneWrong", GetErrorModel("Session lost."));

            HttpContext.Session.Remove(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            return RedirectToAction("Index");
        }
        [HttpPost]
        [Route("{controller}")]
        public IActionResult Index(AuthViewModel model)
        {
            //user wants to logout

            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_EMAIL);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);

            return RedirectToAction("Index","Home");
        }

        [Route("{controller}/consent-nhsnumber")]
        public IActionResult ConsentNhsNumberGP()
        {
            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));


            var keyAnswers = GetAnswerSessionData();
            return View(new ConsentNHSNumberGPPracticeViewModel {  
                ConsentToLookups = keyAnswers.AnsweredNhsNumberGPConsent ? referral.ConsentForGpAndNhsNumberLookup == null ? "null" : referral.ConsentForGpAndNhsNumberLookup == true ? "true" : "false" : "",
                ConsentYNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
            });
        }
        [HttpPost]
        [Route("{controller}/consent-nhsnumber")]
        public IActionResult ConsentNhsNumberGP(ConsentNHSNumberGPPracticeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new ConsentNHSNumberGPPracticeViewModel
                {
                    ConsentToLookups = model.ConsentToLookups,
                    ConsentYNList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            var referral = GetReferralSessionData();
            
            referral.ConsentForGpAndNhsNumberLookup = model.ConsentToLookups == "true";
            SetReferralSessionData(referral);

            if (referral.ConsentForGpAndNhsNumberLookup==false){
                return RedirectToAction("Not-Eligible-For-Service", new { id = "NhsLookupConsent" });
            }

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredNhsNumberGPConsent = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("consent-to-referrer-update");
        }
        [Route("{controller}/consent-to-referrer-update")]
        public IActionResult ConsentReferrerUpdate()
        {
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
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

            var referral = GetReferralSessionData();

            referral.ConsentForReferrerUpdatedWithOutcome = model.ConsentToReferrerUpdate == "true";
            SetReferralSessionData(referral);
                      

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredUpdateReferrerCompletionConsent = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("nhs-number");
        }

        [Route("{controller}/nhs-number")]
        public IActionResult NHSNumber()
        {
            var referral = GetReferralSessionData();

            return View(new NHSNumberViewModel() { NHSNumber = referral.NhsNumber });
        }
        [HttpPost]
        [Route("{controller}/nhs-number")]
        public async Task<IActionResult> NHSNumber(NHSNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new NHSNumberViewModel() { NHSNumber = model.NHSNumber });
            }

            model.NHSNumber = model.NHSNumber.Replace(" ", ""); //replace any white space

            if (!StaticReferralHelper.ValidateNHSNumber(model.NHSNumber))
            {
                ModelState.AddModelError("NHSNumber", "The NHS Number is not valid");
                return View(model);
            }

            try
            {
                //check nhsnumber against api
                var response = await _WmsReferralService.NhsNumberInUseAsync(model.NHSNumber);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await response.Content.ReadAsStringAsync());
                    telemErrors.TryAdd("NHS Number", model.NHSNumber);
                    telemErrors.TryAdd("StatusCode", response.StatusCode.ToString());

                    _telemetry.TrackEvent("GoneWrong:NHSNumberIssue", telemErrors);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            return View("GoneWrong", GetErrorModel("NHS number not valid"));
                        case HttpStatusCode.Conflict:
                            telemErrors.TryGetValue("Detail", out string detailerror);
                            return View("GoneWrong", GetErrorModel("A patient with this NHS number has already been referred to the NHS Digital Weight Management Programme"));
                        case HttpStatusCode.Unauthorized:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        case HttpStatusCode.InternalServerError:
                            return View("GoneWrong", GetErrorModel("An Error has occurred"));
                        default:
                            return View("GoneWrong", GetErrorModel("NHS number not valid"));
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent("GoneWrong:API");
            }


            var referral = GetReferralSessionData();
            referral.NhsNumber = model.NHSNumber;
            SetReferralSessionData(referral);


            return RedirectToAction("gp-practice");
        }
        [Route("{controller}/gp-practice")]
        public IActionResult GPPractice()
        {
            var referral = GetReferralSessionData();

            return View(new GPPracticeViewModel() { ODSCode = referral.ReferringGpPracticeNumber });
        }
        [HttpPost]
        [Route("{controller}/gp-practice")]
        public async Task<IActionResult> GPPractice(GPPracticeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new GPPracticeViewModel() { ODSCode = model.ODSCode });
            }

            var referral = GetReferralSessionData();
            model.ODSCode = model.ODSCode.Trim();
            model.ODSCode = model.ODSCode.ToUpper();

            //simple ods check
            Regex validODSCode = new Regex(Constants.REGEX_ODSCODE_GPPRACTICE);
            if (!validODSCode.IsMatch(model.ODSCode))
            {
                _logger.LogWarning("ODS code not valid: " + model.ODSCode);
                ModelState.AddModelError("ODSCode", "The GP Practice code entered is not valid");
                return View(model); //not valid                
            }

            var gpOrg = await _ODSLookupService.LookupODSCodeAsync(model.ODSCode);
            if (gpOrg.APIStatusCode != 200)
            {
                _logger.LogWarning("ODS code not valid: " + model.ODSCode);
                ModelState.AddModelError("ODSCode", "The GP Practice code entered was not found");
                return View(model); //not valid   
            }
            if (gpOrg.Status != "Active")
            {
                _logger.LogWarning("ODS code valid, status not Active: " + model.ODSCode);
                ModelState.AddModelError("ODSCode", "The GP Practice code entered was found but is not active");
                return View(model); //not valid  
            }


                referral.ReferringGpPracticeNumber = model.ODSCode;
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("gp-practice-confirm");
        }
        [Route("{controller}/gp-practice-confirm")]
        public async Task<IActionResult> GPPracticeConfirm()
        {
            var referral = GetReferralSessionData();
            var gpOrg = await _ODSLookupService.LookupODSCodeAsync(referral.ReferringGpPracticeNumber);

            return View(new GPPracticeViewModel() { ODSCode = referral.ReferringGpPracticeNumber, GPOrg = gpOrg });
        }
        [HttpPost]
        [Route("{controller}/gp-practice-confirm")]
        public IActionResult GPPracticeConfirm(GPPracticeViewModel model)
        {

            var referral = GetReferralSessionData();
            referral.ReferringGpPracticeName = model.GPOrg.Name;
            referral.ReferringGpPracticeNumber = model.ODSCode;
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("email-address");
        }

        [Route("{controller}/email-address")]
        public IActionResult EmailAddress()
        {
            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));


            return View(new EmailAddressViewModel { Email = referral.Email });
        }
        [HttpPost]
        [Route("{controller}/email-address")]
        public IActionResult EmailAddress(EmailAddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new EmailAddressViewModel { Email = model.Email });
            }

            model.Email = model.Email.Trim();

            Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAILADDRESS);
            if (!validDomain.IsMatch(model.Email))
            {                
                ModelState.AddModelError("Email", "Email address not valid");
                return View(new EmailAddressViewModel { Email = model.Email });
            }

            var referral = GetReferralSessionData();
            referral.Email = model.Email;
            SetReferralSessionData(referral);

            //in case this is referral > 1
            ResetReferralSubmitted();

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("hypertension");
        }

        

        [Route("{controller}/hypertension")]
        public IActionResult Hypertension()
        {
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new HypertensionViewModel
            {
                Hypertension = keyAnswers.AnsweredHypertension ? referral.HasHypertension == null ? "null" : referral.HasHypertension == true ? "true" : "false" : "",
                HypertensionList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
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
                    HypertensionList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));

            referral.HasHypertension = model.Hypertension == "true" ? true : model.Hypertension == "false" ? false : null;
            SetReferralSessionData(referral);

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
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new DiabetesViewModel
            {
                DiabetesTypeOne = keyAnswers.AnsweredDiabetesType1 ? referral.HasDiabetesType1 == null ? "null" : referral.HasDiabetesType1 == true ? "true" : "false" : "",
                DiabetesList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
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
                    DiabetesList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));

            referral.HasDiabetesType1 = model.DiabetesTypeOne == "true" ? true : model.DiabetesTypeOne == "false" ? false : null;
            SetReferralSessionData(referral);

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
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();

            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            return View(new DiabetesTypeTwoViewModel
            {
                DiabetesTypeTwo = keyAnswers.AnsweredDiabetesType2 ? referral.HasDiabetesType2 == null ? "null" : referral.HasDiabetesType2 == true ? "true" : "false" : "",
                DiabetesList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
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
                    DiabetesList = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.HasDiabetesType2 = model.DiabetesTypeTwo == "true" ? true : model.DiabetesTypeTwo == "false" ? false : null;
            SetReferralSessionData(referral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredDiabetesType2 = true;
            SetAnswerSessionData(keyAnswers);

            if (referral.HasHypertension==false && 
                referral.HasDiabetesType1==false && 
                referral.HasDiabetesType2 ==false && 
                keyAnswers.AnsweredDiabetesType1 && 
                keyAnswers.AnsweredDiabetesType2 && 
                keyAnswers.AnsweredHypertension)
            {
                return RedirectToAction("Not-Eligible-For-Service", new { id = "Health" });
            }

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("height");
        }

        [Route("{controller}/height")]
        public IActionResult Height()
        {
            var referral = GetReferralSessionData();
            return View(new HeightViewModel { Height = referral.HeightCm });
        }
        [HttpPost]
        [Route("{controller}/height")]
        public IActionResult Height(HeightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
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

            referral.HeightCm = model.Height;
            SetReferralSessionData(referral);

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

            var referral = GetReferralSessionData();
            if (referral.Email == null)
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
            if (model.Weight > 300)
            {
                ModelState.AddModelError("WeightError", "Weight must be equal or lower than 300kg");
                return View(model);
            }
            if (model.Weight < 35)
            {
                ModelState.AddModelError("WeightError", "Weight must be equal or greater than 35kg");
                return View(model);
            }

            referral.WeightKg = model.Weight;

            //weight date taken
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
            {
                referral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

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

            SetReferralSessionData(referral);


            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }
        [Route("{controller}/height-imperial")]
        public IActionResult HeightImperial()
        {
            var referral = GetReferralSessionData();
            return View(_referralCalcs.ConvertCm(referral.HeightCm));
        }
        [HttpPost]
        [Route("{controller}/height-imperial")]
        public IActionResult HeightImperial(HeightImperialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
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

            referral.HeightCm = heightCm;
            SetReferralSessionData(referral);

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

            var referral = GetReferralSessionData();
            if (referral.Email == null)
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
            if (weight > 300)
            {
                ModelState.AddModelError("WeightError", "Weight must be lower than 47st and 3.4lbs");
                return View(model);
            }
            if (weight < 35)
            {
                ModelState.AddModelError("WeightError", "Weight must be greater than 5st and 7.1lbs");
                return View(model);
            }

            referral.WeightKg = weight;

            //weight date taken
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-GB");
            string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

            if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
            {
                referral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

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
            SetReferralSessionData(referral);


            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("select-ethnicity-group");
        }


        //[Route("{controller}/weight-date-taken")]
        //public IActionResult WeightDateTaken()
        //{
        //    var referral = GetReferralSessionData();

        //    if (referral.DateOfBmiAtRegistration == null)
        //        return View();

        //    return View(new WeightDateTakenViewModel
        //    {
        //        Day = referral.DateOfBmiAtRegistration.Value.Day,
        //        Month = referral.DateOfBmiAtRegistration.Value.Month,
        //        Year = referral.DateOfBmiAtRegistration.Value.Year
        //    });
        //}
        //[HttpPost]
        //[Route("{controller}/weight-date-taken")]
        //public IActionResult WeightDateTaken(WeightDateTakenViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(new WeightDateTakenViewModel
        //        {
        //            Day = model.Day,
        //            Month = model.Month,
        //            Year = model.Year
        //        });
        //    }

        //    var referral = GetReferralSessionData();
        //    if (referral.Email == null)
        //    {
        //        //error, redirect
        //        _telemetry.TrackEvent("GoneWrong:Inactivity");
        //        return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
        //    }

        //    CultureInfo culture;
        //    culture = CultureInfo.CreateSpecificCulture("en-GB");
        //    string dateOfweight = model.Day + "/" + model.Month + "/" + model.Year + " 00:00:00";

        //    if (DateTime.TryParse(dateOfweight, culture, DateTimeStyles.None, out DateTime parsedWeightdate))
        //    {
        //        referral.DateOfBmiAtRegistration = new DateTimeOffset(parsedWeightdate.Year, parsedWeightdate.Month, parsedWeightdate.Day, 0, 0, 0, TimeSpan.Zero);

        //        if (parsedWeightdate < DateTime.Now.AddMonths(-24))
        //        {
        //            //if past
        //            ModelState.AddModelError("DateError", "Date weight was taken must not be older than 2 years");
        //            return View(model);
        //        }
        //        if (parsedWeightdate > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59))
        //        {
        //            //if future
        //            ModelState.AddModelError("DateError", "Date weight was taken must be today or in the past");
        //            return View(model);
        //        }

        //    }
        //    else
        //    {
        //        //invalid date
        //        ModelState.AddModelError("DateError", "Date must be a real date");
        //        return View(model);
        //    }
        //    SetReferralSessionData(referral);

        //    if (ReferralCompleted())
        //        return RedirectToAction("check-answers");

        //    return RedirectToAction("select-ethnicity-group");
        //}
        [Route("{controller}/select-ethnicity-group")]
        public async Task<IActionResult> EthnicityGroup()
        {
            var referral = GetReferralSessionData();
            var groupName = "";
            if (referral.ServiceUserEthnicityGroup != "")
            {
                //ethnicity has been set already       
                groupName = referral.ServiceUserEthnicityGroup;
            }

            var ethnicities = await _WmsReferralService.GetEthnicityGroupList(APIENDPOINT);
            if (ethnicities.Count() == 0)
            {
                _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", referral.Email }, { "Error", "Ethnicity API null" } });
                //error, redirect
                return View("GoneWrong", GetErrorModel("Service Error"));
            }

            return View(new EthnicityViewModel { EthnicityGroupList = ethnicities, ReferralEthnicityGroup = groupName });
        }
        [HttpPost]
        [Route("{controller}/select-ethnicity-group")]
        public async Task<IActionResult> EthnicityGroup(EthnicityViewModel model)
        {
            ModelState.Remove("referralEthnicity");

            if (!ModelState.IsValid)
            {
                return View(new EthnicityViewModel { EthnicityGroupList = await _WmsReferralService.GetEthnicityGroupList(APIENDPOINT), ReferralEthnicityGroup = model.ReferralEthnicityGroup });
            }

            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));

            if (model.ReferralEthnicityGroup == "The patient does not want to disclose their ethnicity")
            {
                referral.Ethnicity = "Other"; //triagename
                referral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup; //groupname
                referral.ServiceUserEthnicity = model.ReferralEthnicityGroup; //displayname
                SetReferralSessionData(referral);

                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(referral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

                return RedirectToAction("date-of-birth");
            }

            referral.ServiceUserEthnicityGroup = model.ReferralEthnicityGroup;
            SetReferralSessionData(referral);

            return RedirectToAction("Select-Ethnicity", new { id = model.ReferralEthnicityGroup });
        }
        [Route("{controller}/select-ethnicity/")]
        [Route("{controller}/select-ethnicity/{id}")]
        public async Task<IActionResult> Ethnicity(string id)
        {

            var referral = GetReferralSessionData();
            if (id == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:No Ethnicity");
                return View("GoneWrong", GetErrorModel("Something went wrong"));
            }

            return View(new EthnicityViewModel
            {
                EthnicityGroupList = await _WmsReferralService.GetEthnicityGroupList(APIENDPOINT),
                EthnicityGroupDescription = id,
                EthnicityList = await _WmsReferralService.GetEthnicityMembersList(id,APIENDPOINT),
                ReferralEthnicityGroup = id,
                SelectedEthnicity = referral.ServiceUserEthnicity,
                ReferralEthnicity = referral.ServiceUserEthnicity
            });
        }
        [HttpPost]
        [Route("{controller}/select-ethnicity")]
        public async Task<IActionResult> Ethnicity(EthnicityViewModel model)
        {
            ModelState.Remove("referralEthnicityGroup");
            if (!ModelState.IsValid)
            {
                return View(new EthnicityViewModel
                {
                    EthnicityGroupList = await _WmsReferralService.GetEthnicityGroupList(APIENDPOINT),
                    EthnicityGroupDescription = model.ReferralEthnicityGroup,
                    EthnicityList = await _WmsReferralService.GetEthnicityMembersList(model.ReferralEthnicityGroup,APIENDPOINT),
                    ReferralEthnicityGroup = model.ReferralEthnicityGroup,
                    SelectedEthnicity = model.ReferralEthnicity
                });
            }

            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));

            var ethnicities = await _WmsReferralService.GetEthnicities(APIENDPOINT);
            if (ethnicities == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:API", new Dictionary<string, string> { { "Email", referral.Email }, { "Error", "Ethnicity API null" } });
                return View("GoneWrong", GetErrorModel("Service error"));
            }

            referral.Ethnicity = ethnicities.ToList().Where(w => w.DisplayName == model.ReferralEthnicity).First().TriageName;
            referral.ServiceUserEthnicity = model.ReferralEthnicity; //displayname
            SetReferralSessionData(referral);

            //bmi check
            var bmiCheck = _referralCalcs.BmiEligibility(referral);
            if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
            if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("date-of-birth");
        }
        [Route("{controller}/date-of-birth")]
        public IActionResult DateofBirth()
        {
            var referral = GetReferralSessionData();

            if (referral.DateOfBirth == null)
                return View(new DateOfBirthViewModel
                {                    
                    BackActionRoute = referral.ServiceUserEthnicityGroup
                });

            return View(new DateOfBirthViewModel
            {
                Day = referral.DateOfBirth.Value.Day,
                Month = referral.DateOfBirth.Value.Month,
                Year = referral.DateOfBirth.Value.Year,                
                BackActionRoute = referral.ServiceUserEthnicityGroup
            });
        }
        [HttpPost]
        [Route("{controller}/date-of-birth")]
        public IActionResult DateofBirth(DateOfBirthViewModel model)
        {
            var referral = GetReferralSessionData();
            if (!ModelState.IsValid)
            {
                return View(new DateOfBirthViewModel
                {
                    Day = model.Day,
                    Month = model.Month,
                    Year = model.Year,                    
                    BackActionRoute = referral.ServiceUserEthnicityGroup
                });
            }
                        
            if (referral.Email == null)
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
                referral.DateOfBirth = new DateTimeOffset(outdob.Year, outdob.Month, outdob.Day, 0, 0, 0, TimeSpan.Zero);
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

            }
            else
            {
                ModelState.AddModelError("DateError", "Date must be a real date");
                return View(model);
            }
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("family-name");
        }
        [Route("{controller}/family-name")]
        public IActionResult FamilyName()
        {
            var referral = GetReferralSessionData();
            return View(new FamilyNameViewModel { FamilyName = referral.FamilyName, BackActionRoute = referral.ServiceUserEthnicityGroup });
        }
        [HttpPost]
        [Route("{controller}/family-name")]
        public IActionResult FamilyName(FamilyNameViewModel model)
        {
            var referral = GetReferralSessionData();

            if (!ModelState.IsValid)
            {
                return View(new FamilyNameViewModel { FamilyName = model.FamilyName, BackActionRoute = referral.ServiceUserEthnicityGroup });
            }


            referral.FamilyName = StaticReferralHelper.StringCleaner(model.FamilyName);
            
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("given-name");
        }
        [Route("{controller}/given-name")]
        public IActionResult GivenName()
        {
            var referral = GetReferralSessionData();
            return View(new GivenNameViewModel { GivenName = referral.GivenName });
        }
        [HttpPost]
        [Route("{controller}/given-name")]
        public IActionResult GivenName(GivenNameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.GivenName = StaticReferralHelper.StringCleaner(model.GivenName);
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("Address");
        }
        [Route("{controller}/address")]
        public IActionResult Address()
        {
            var referral = GetReferralSessionData();
            return View(new AddressViewModelV1
            {
                Address1 = referral.Address1,
                Address2 = referral.Address2,
                Address3 = referral.Address3,
                Postcode = referral.Postcode

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

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            if (model.Postcode != "" && referral.Postcode != model.Postcode.ToUpper())
            {
                //user entered different postcode - revalidate
                model.UserWarned = false;
            }

            referral.Address1 = StaticReferralHelper.StringCleaner(model.Address1);
            referral.Address2 = StaticReferralHelper.StringCleaner(model.Address2);
            referral.Address3 = StaticReferralHelper.StringCleaner(model.Address3);
            referral.Postcode = StaticReferralHelper.StringCleaner(model.Postcode.ToUpper());
            model.Postcode = model.Postcode.ToUpper();
            SetReferralSessionData(referral);

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
            var referral = GetReferralSessionData();
            return View(new MobileViewModel { Mobile = referral.Mobile });
        }
        [HttpPost]
        [Route("{controller}/mobile")]
        public IActionResult Mobile(MobileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }


            referral.Mobile = model.Mobile;
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("telephone");
        }
        [Route("{controller}/telephone")]
        public IActionResult Telephone()
        {
            var referral = GetReferralSessionData();
            return View(new TelephoneViewModel { Telephone = referral.Telephone });
        }
        [HttpPost]
        [Route("{controller}/telephone")]
        public IActionResult Telephone(TelephoneViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.Telephone = model.Telephone;
            SetReferralSessionData(referral);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("sex");
        }
        public IActionResult Sex()
        {
            var referral = GetReferralSessionData();
            return View(new SexViewModel { Sex = referral.Sex, Sexes = StaticReferralHelper.GetSexes() });
        }
        [HttpPost]
        [Route("{controller}/sex")]
        public IActionResult Sex(SexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new SexViewModel { Sex = model.Sex, Sexes = StaticReferralHelper.GetSexes() });
            }

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.Sex = model.Sex;
            SetReferralSessionData(referral);

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
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            return View(new LearningDisabilityViewModel
            {
                LearningDisability = keyAnswers.AnsweredLearningDisability ? referral.HasALearningDisability == null ? "null" : referral.HasALearningDisability == true ? "true" : "false" : "",
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

            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.HasALearningDisability = model.LearningDisability == "true" ? true : model.LearningDisability == "false" ? false : null;
            SetReferralSessionData(referral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredLearningDisability = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("phonecall-required");
        }
        [Route("{controller}/phonecall-required")]
        public IActionResult VulnerablePatient()
        {
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();
            var ynList = StaticReferralHelper.GetYNList().ToList();
            
            ynList.Remove(new KeyValuePair<string, string>("null", "Don't know / Prefer not to say"));            
            return View(new VulnerablePatientViewModel
            {
                VulnerablePatient = keyAnswers.AnsweredPatientVulnerable ? referral.IsVulnerable == null ? "null" : referral.IsVulnerable == true ? "true" : "false" : "",
                VulnerableDescription = referral.VulnerableDescription,
                VulnerablePatientList = ynList
            });
        }
        [HttpPost]
        [Route("{controller}/phonecall-required")]
        public IActionResult VulnerablePatient(VulnerablePatientViewModel model)
        {
            var ynList = StaticReferralHelper.GetYNList().ToList();
            ynList.Remove(new KeyValuePair<string, string>("null", "Don't know / Prefer not to say"));
                        
            if (!ModelState.IsValid)
            {                
                return View(new VulnerablePatientViewModel
                {
                    VulnerablePatient = model.VulnerablePatient,
                    VulnerableDescription = model.VulnerableDescription,
                    VulnerablePatientList = ynList
                });
            }


            var referral = GetReferralSessionData();
            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            referral.IsVulnerable = model.VulnerablePatient == "true" ? true : model.VulnerablePatient == "false" ? false : null;
            referral.VulnerableDescription = model.VulnerablePatient == "true" ? "Service user needs a phone call to help them." : null;
            SetReferralSessionData(referral);

            //mark answered
            var keyAnswers = GetAnswerSessionData();
            keyAnswers.AnsweredPatientVulnerable = true;
            SetAnswerSessionData(keyAnswers);

            if (ReferralCompleted())
                return RedirectToAction("check-answers");

            return RedirectToAction("check-answers");
        }

        [Route("{controller}/check-answers")]
        public IActionResult CheckAnswers()
        {
            var referral = GetReferralSessionData();
            var keyAnswers = GetAnswerSessionData();

            if (referral.Email == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                return View("GoneWrong", GetErrorModel("User timed out for inactivity"));
            }

            //set imperial
            keyAnswers.HeightImperial = _referralCalcs.ConvertCm(referral.HeightCm) ?? keyAnswers.HeightImperial;
            keyAnswers.WeightImperial = _referralCalcs.ConvertKg(referral.WeightKg) ?? keyAnswers.WeightImperial;
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


            return View(new CheckAnswersViewModel { Referral = referral, KeyAnswer = keyAnswers });
        }
        [HttpPost]
        [Route("{controller}/check-answers")]
        public async Task<IActionResult> CheckAnswers(CheckAnswersViewModel model)
        {
            try
            {
                var referral = GetReferralSessionData();
                referral = (PharmacyReferral)StaticReferralHelper.FinalCheckAnswerChecks(referral);

                //bmi check
                var bmiCheck = _referralCalcs.BmiEligibility(referral);
                if (bmiCheck == HttpStatusCode.PreconditionFailed) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI" });
                if (bmiCheck == HttpStatusCode.RequestEntityTooLarge) //not eligible
                    return RedirectToAction("Not-Eligible-For-Service", new { id = "BMI-Limit" });

                if (referral.ReferringGpPracticeNumber == null)
                {
                    referral.ReferringGpPracticeNumber = "V81999";
                }
                referral.CalculatedBmiAtRegistration = _referralCalcs.CalcBmi(referral);

                //post to api
                var result = await _WmsReferralService.AddPharmacyReferralAsync(referral);

                //if success
                if (result.StatusCode == HttpStatusCode.OK)
                {

                    //emailconfirmation
                    await EmailConfirmation(referral.ReferringPharmacyEmail, referral.GivenName + " " + referral.FamilyName);

                    //mark referral submitted
                    var keyAnswers = GetAnswerSessionData();
                    keyAnswers.ReferralSubmitted = true;
                    SetAnswerSessionData(keyAnswers);

                    return RedirectToAction("complete");
                }


                //problem
                var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await result.Content.ReadAsStringAsync(), referral);
                telemErrors.Add("PharmEmail", referral.ReferringPharmacyEmail);
                telemErrors.Add("PharmODS", referral.ReferringPharmacyODSCode);
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
        private async Task EmailConfirmation(string emailaddress, string patientname) 
        {

            await _emailSender.SendEmailAsync(
                       emailaddress,
                       "NHS Digital WMS Pharmacy Referral",
                       "<p style='font-family:Arial'>"+patientname+" has been successfully referred.</p>",
                       patientname + " has been successfully referred." + Environment.NewLine + ""
                       );

        }
        [Route("{controller}/complete")]
        public IActionResult Complete()
        {
            //remove sessions
            HttpContext.Session.Remove(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
            HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            
            return View("Complete");
        }
        [Route("{controller}/not-eligible-for-service/{id}")]
        public IActionResult NotEligible(string id)
        {

            var referral = GetReferralSessionData();
            _telemetry.TrackEvent("NotEligible", new Dictionary<string, string> {
                { "Age",_referralCalcs.CalcAge(referral).ToString() ?? "NotSet" },
                { "BMI",_referralCalcs.CalcBmi(referral).ToString() ?? "NotSet" },
                { "Reason", id }
            });

            
            if (id != "Age") //remove sessions, but not for incorrect age
            { 
                HttpContext.Session.Remove(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
                HttpContext.Session.Remove(SESSION_KEY_ANSWER_TOKEN);
            }

            return id switch
            {
                "BMI" => View("NotEligibleBMI", new NotEligibleViewModel { Message = "" }),
                "BMI-Limit" => View("NotEligibleBMILimit", new NotEligibleViewModel { Message = "" }),
                "Email" => View("NotEligibleEmail", new NotEligibleViewModel { Message = "" }),
                "Age" => View("NotEligibleAge", new NotEligibleViewModel { Message = "" }),
                "Health" => View("NotEligibleHealth", new NotEligibleViewModel { Message = "" }),
                "NhsLookupConsent" => View("NotEligibleConsent", new NotEligibleViewModel { Message = "" }),
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
                  await _WmsReferralService.GetStaffRolesAsync();

                foreach (string staffRole in businessModel.OrderBy(o => o.DisplayOrder).Select(s => s.DisplayName))
                {
                    staffRoleList.Add(new KeyValuePair<string, string>(staffRole, staffRole));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return staffRoleList.ToList();
        }
        

        private PharmacyReferral GetReferralSessionData()
        {
            try
            {
                var srSession = HttpContext.Session.Get<PharmacyReferral>(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
                if (srSession == null)
                { //if its null midway may need to start again
                    var authSession = HttpContext.Session.Get<AuthViewModel>(SESSION_KEY_EMAIL);
                    if (authSession==null)
                        return new PharmacyReferral { ReferringPharmacyODSCode = "-1" };
                    if (!authSession.IsAuthorised)
                        return new PharmacyReferral { ReferringPharmacyODSCode = "-2" };
                    return new PharmacyReferral
                    {
                        ReferringPharmacyODSCode = authSession.ODSCode,
                        ReferringPharmacyEmail = authSession.EmailAddress,
                        DateOfReferral = DateTime.UtcNow
                    };
                }
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new PharmacyReferral {  };
        }

        private void SetReferralSessionData(PharmacyReferral referral)
        {
            try
            {
                HttpContext.Session.Set<PharmacyReferral>(SESSION_KEY_PHARMACYREFERRAL_TOKEN, referral);
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
                && answers.AnsweredPatientVulnerable==true
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
                        AnsweredUpdateReferrerCompletionConsent=false,
                        AnsweredNhsNumberGPConsent = false,
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
            _telemetry.TrackTrace(message);
            return new ErrorViewModel()
            {
                RequestId = "",
                TraceId = traceid,
                Message = $"Error: {message}"
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

        private IActionResult IsSessionAvailable()
        {
            var referral = GetReferralSessionData();
            if (referral.ReferringPharmacyODSCode == "-1")//null
                return View("GoneWrong", GetErrorModel("Session lost."));
            if (referral.ReferringPharmacyODSCode == "-2")//not authorised
                return View("GoneWrong", GetErrorModel("Not Authorised"));

            return null;
        }
    }
}
