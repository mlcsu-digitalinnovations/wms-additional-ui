using EllipticCurve;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsSurveys.Web.Data;
using WmsSurveys.Web.Helpers;

namespace WmsSurveys.Web.Controllers
{
    public class NonCompleterSelfReferralController : SessionControllerBase
    {
        private readonly ILogger<NonCompleterSelfReferralController> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IQuestionnaireData _sessionData;
        private readonly string _questionnaireType = "ncsr";

        public NonCompleterSelfReferralController(ILogger<NonCompleterSelfReferralController> logger,
             TelemetryClient telemetry, IQuestionnaireData sessionData)
        {
            _logger = logger;
            _telemetry = telemetry;
            _sessionData = sessionData;

            var questionnaire = _sessionData.GetSessionData();
            if (questionnaire.QuestionnaireRequested != _questionnaireType)
            {                
                var errors = new Dictionary<string, string>() { 
                    { "UserRef", questionnaire.NotificationKey }, 
                    { "Questionnaire Requested", _questionnaireType },
                    { "Questionnaire Assigned", questionnaire.QuestionnaireRequested }
                };
                
                questionnaire.NotificationKey = "";
                questionnaire.QuestionnaireRequested = "";
                _sessionData.SetSessionData(questionnaire);

                //log inconsistency
                _telemetry.TrackEvent("Inconsistency", errors);
            }
        }
        [Route("ncsr/Q1")]
        [Route("ncsr/Q2")]
        [Route("ncsr/Q3")]
        [Route("ncsr/Q4")]
        [Route("ncsr/Q5")]
        [Route("ncsr/Q6")]
        [Route("ncsr/Q7")]
        [Route("ncsr/index")]
        [Route("ncsr/begin")]
        public IActionResult GoneWrong()
        {
            //log inconsistency
            _telemetry.TrackEvent("Notification key missing");
            return Redirect("/Error");
        }
        [Route("ncsr/{id}/begin")]
        public IActionResult Index(string id = "")
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            return View(survey);
        }

        [Route("ncsr/q1/{id}")]
        [Route("ncsr/{id}/q1")]
        public IActionResult Q1(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(1, survey, StaticReferralHelper.GetSentimentList());

            return View(vm);
        }
        [Route("ncsr/q1/{id}")]
        [Route("ncsr/{id}/q1")]
        [HttpPost]
        public IActionResult Q1(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionAresponses");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");

            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    QuestionBresponse = model.QuestionBresponse,
                    QuestionCresponse = model.QuestionCresponse,
                    Responses = StaticReferralHelper.GetSentimentList()
                });
            }


            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 1);
            _sessionData.SetSessionData(survey);


            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q2");
        }
        [Route("ncsr/q2/{id}")]
        [Route("ncsr/{id}/q2")]
        public IActionResult Q2(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(2, survey, StaticReferralHelper.GetYNList().Where(w => w.Key != "null"));
            vm.ProviderName = survey.ProviderName;
            return View(vm);
        }
        [Route("ncsr/q2/{id}")]
        [Route("ncsr/{id}/q2")]
        [HttpPost]
        public IActionResult Q2(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionAresponses");
            ModelState.Remove("QuestionBresponse");
            ModelState.Remove("QuestionCresponse");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");
            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    QuestionBresponse = model.QuestionBresponse,
                    QuestionCresponse = model.QuestionCresponse,
                    QuestionDresponse = model.QuestionDresponse,
                    QuestionEresponse = model.QuestionEresponse,
                    QuestionFresponse = model.QuestionFresponse,
                    QuestionGresponse = model.QuestionGresponse,
                    QuestionHresponse = model.QuestionHresponse,
                    Responses = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            //save data to session?
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 2);
            _sessionData.SetSessionData(survey);

            if (model.QuestionAresponse == "true")
                return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q4");
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q3");
        }
        [Route("ncsr/q3/{id}")]
        [Route("ncsr/{id}/q3")]
        public IActionResult Q3(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(3, survey, StaticReferralHelper.GetSurveyQ3List());
            if (vm.QuestionAresponses == null || vm.QuestionAresponses?.Count == 0)
                vm.QuestionAresponses = new List<CheckBox>(StaticReferralHelper.GetSurveyQ3List().Select(s => new CheckBox { Selected = false, Text = s.Key, Value = s.Value }));

            return View(vm);
        }
        [Route("ncsr/q3/{id}")]
        [Route("ncsr/{id}/q3")]
        [HttpPost]
        public IActionResult Q3(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionAresponse");
            ModelState.Remove("QuestionBresponse");
            ModelState.Remove("QuestionCresponse");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");
            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    Responses = StaticReferralHelper.GetSurveyQ3List()
                });
            }

            if (!model.QuestionAresponses.Where(w => w.Selected).Any())
            {
                ModelState.TryAddModelError("QuestionAresponse", "A response is required");
                return View(model);
            }

            if (StaticReferralHelper.WordCount(model.QuestionAresponse) > 100)
            {
                ModelState.TryAddModelError("QuestionAresponse", "Your other response is too long");
                return View(model);
            }

            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 3);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q5");
        }
        [Route("ncsr/q4/{id}")]
        [Route("ncsr/{id}/q4")]
        public IActionResult Q4(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(4, survey, StaticReferralHelper.GetSurveyQ4List());
            if (vm.QuestionAresponses == null || vm.QuestionAresponses?.Count == 0)
                vm.QuestionAresponses = new List<CheckBox>(StaticReferralHelper.GetSurveyQ4List().Select(s => new CheckBox { Selected = false, Text = s.Key, Value = s.Value }));

            return View(vm);
        }
        [Route("ncsr/q4/{id}")]
        [Route("ncsr/{id}/q4")]
        [HttpPost]
        public IActionResult Q4(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionAresponse");
            ModelState.Remove("QuestionBresponse");
            ModelState.Remove("QuestionCresponse");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");

            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    Responses = StaticReferralHelper.GetSurveyQ4List()
                });
            }

            if (!model.QuestionAresponses.Where(w => w.Selected).Any())
            {
                ModelState.TryAddModelError("QuestionAresponse", "A response is required");
                return View(model);
            }

            if (StaticReferralHelper.WordCount(model.QuestionAresponse) > 100)
            {
                ModelState.TryAddModelError("QuestionAresponse", "Your other response is too long");
                return View(model);
            }

            //save data to session?
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 4);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q5");
        }

        [Route("ncsr/q5/{id}")]
        [Route("ncsr/{id}/q5")]
        public IActionResult Q5(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(5, survey, StaticReferralHelper.GetSentimentList());

            var q2 = survey.QuestionAnswers.Where(w => w.QuestionId == 2)?.FirstOrDefault();
            if (q2 != null)
                vm.GoBack = q2.QuestionAresponse == "true" ? "Q4" : "Q3";

            return View(vm);
        }
        [Route("ncsr/q5/{id}")]
        [Route("ncsr/{id}/q5")]
        [HttpPost]
        public IActionResult Q5(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionBresponse");
            ModelState.Remove("QuestionCresponse");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");

            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    Responses = StaticReferralHelper.GetSentimentList()
                });
            }

            if (StaticReferralHelper.WordCount(model.QuestionAresponse) > 100)
            {
                ModelState.TryAddModelError("QuestionAresponse", "Response is too long");
                return View(model);
            }

            //save data to session?

            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 5);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q6");
        }
        [Route("ncsr/q6/{id}")]
        [Route("ncsr/{id}/q6")]
        public IActionResult Q6(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(6, survey, StaticReferralHelper.GetYNList().Where(w => w.Key != "null"));
            return View(vm);
        }
        [Route("ncsr/q6/{id}")]
        [Route("ncsr/{id}/q6")]
        [HttpPost]
        public IActionResult Q6(QuestionsViewModel model, string id)
        {
            ModelState.Remove("QuestionAresponse");
            ModelState.Remove("QuestionBresponse");
            ModelState.Remove("QuestionCresponse");
            ModelState.Remove("QuestionDresponse");
            ModelState.Remove("QuestionEresponse");
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");
            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    Responses = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            if (StaticReferralHelper.WordCount(model.QuestionAresponse) > 100)
            {
                ModelState.TryAddModelError("QuestionAresponse", "Your response is too long");
                return View(model);
            }

            //save data to session?
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 6);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q7");
        }
        [Route("ncsr/q7/{id}")]
        [Route("ncsr/{id}/q7")]
        public IActionResult Q7(string id)
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(7, survey, StaticReferralHelper.GetYNList().Where(w => w.Key != "null"));
            return View(vm);
        }
        [Route("ncsr/q7/{id}")]
        [Route("ncsr/{id}/q7")]
        [HttpPost]
        public IActionResult Q7(QuestionsViewModel model, string id)
        {
            if (model.QuestionAresponse == "false")
            {
                ModelState.Remove("QuestionBresponse");
                ModelState.Remove("QuestionCresponse");
                ModelState.Remove("QuestionDresponse");
                ModelState.Remove("QuestionEresponse");
            }
            ModelState.Remove("QuestionFresponse");
            ModelState.Remove("QuestionGresponse");
            ModelState.Remove("QuestionHresponse");
            if (!ModelState.IsValid)
            {
                return View(new QuestionsViewModel
                {
                    QuestionAresponse = model.QuestionAresponse,
                    Responses = StaticReferralHelper.GetYNList().Where(w => w.Key != "null")
                });
            }

            if (model.QuestionAresponse == "true")
            {
                //do some error checking
                var email = model.QuestionBresponse;
                var tel = model.QuestionCresponse;

                Regex validDomain = new Regex(Constants.REGEX_WMS_VALID_EMAILADDRESS);
                if (!validDomain.IsMatch(email))
                {
                    ModelState.AddModelError("QuestionBresponse", "Email address not valid");
                    model.Responses = StaticReferralHelper.GetYNList().Where(w => w.Key != "null");
                    return View(model);
                }

                Regex validTel = new Regex(Constants.REGEX_MOBILE_PHONE_UK);
                if (!validTel.IsMatch(tel))
                {
                    ModelState.AddModelError("QuestionCresponse", "Mobile number not valid");
                    model.Responses = StaticReferralHelper.GetYNList().Where(w => w.Key != "null");
                    return View(model);
                }

                model.QuestionDresponse = StaticReferralHelper.StringCleaner(model.QuestionDresponse);
                model.QuestionEresponse = StaticReferralHelper.StringCleaner(model.QuestionEresponse);
            }

            //save data to session?

            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 7);
            _sessionData.SetSessionData(survey);
            return Redirect("/u/" + id + "/complete");
        }



    }
}
