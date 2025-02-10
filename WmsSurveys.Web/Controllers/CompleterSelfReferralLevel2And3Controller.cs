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
    public class CompleterSelfReferralLevel2And3Controller : SessionControllerBase
    {        
        private readonly ILogger<CompleterSelfReferralLevel2And3Controller> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IQuestionnaireData _sessionData;
        private readonly string _questionnaireType = "csrl23";
        public CompleterSelfReferralLevel2And3Controller(ILogger<CompleterSelfReferralLevel2And3Controller> logger,
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
        [Route("csrl23/Q1")]
        [Route("csrl23/Q2")]
        [Route("csrl23/Q3")]
        [Route("csrl23/Q4")]
        [Route("csrl23/Q5")]
        [Route("csrl23/Q6")]
        [Route("csrl23/Q7")]
        [Route("csrl23/index")]
        [Route("csrl23/begin")]
        public IActionResult GoneWrong()
        {
            //log inconsistency
            _telemetry.TrackEvent("Notification key missing");
            return Redirect("/Error");
        }
        [Route("csrl23/{id}/begin")]       
        public IActionResult Index(string id = "")
        {
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            return View(survey);
        }
        
        [Route("csrl23/q1/{id}")]
        [Route("csrl23/{id}/q1")]
        public IActionResult Q1(string id)
        {            
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(1, survey, StaticReferralHelper.GetSentimentList());

            return View(vm);
        }
        [Route("csrl23/q1/{id}")]
        [Route("csrl23/{id}/q1")]
        [HttpPost]
        public IActionResult Q1(QuestionsViewModel model, string id)
        {
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
            survey = StaticQuestionnaireHelper.SaveAnswers(model,survey,1);
            _sessionData.SetSessionData(survey);

            
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q2");
        }
        [Route("csrl23/q2/{id}")]
        [Route("csrl23/{id}/q2")]
        public IActionResult Q2(string id)
        {            
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(2, survey, StaticReferralHelper.GetSentimentList());
            return View(vm);
        }
        [Route("csrl23/q2/{id}")]
        [Route("csrl23/{id}/q2")]
        [HttpPost]
        public IActionResult Q2(QuestionsViewModel model, string id)
        {
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
                    Responses = StaticReferralHelper.GetSentimentList()
                });
            }

            //save data to session?
            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 2);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q3");
        }
        [Route("csrl23/q3/{id}")]
        [Route("csrl23/{id}/q3")]
        public IActionResult Q3(string id)
        {            
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(3, survey, StaticReferralHelper.GetSentimentList());
            return View(vm);
        }
        [Route("csrl23/q3/{id}")]
        [Route("csrl23/{id}/q3")]
        [HttpPost]
        public IActionResult Q3(QuestionsViewModel model, string id)
        {
            
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

            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 3);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q4");
        }
        [Route("csrl23/q4/{id}")]
        [Route("csrl23/{id}/q4")]
        public IActionResult Q4(string id)
        {            
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(4, survey, StaticReferralHelper.GetExperienceList());
            return View(vm);
        }
        [Route("csrl23/q4/{id}")]
        [Route("csrl23/{id}/q4")]
        [HttpPost]
        public IActionResult Q4(QuestionsViewModel model, string id)
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
                    Responses = StaticReferralHelper.GetExperienceList()
                });
            }

            //save data to session?

            var survey = _sessionData.GetSessionData();
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id +"/sl");
            survey = StaticQuestionnaireHelper.SaveAnswers(model, survey, 4);
            _sessionData.SetSessionData(survey);
            return Redirect("/" + survey.QuestionnaireRequested + "/" + survey.NotificationKey + "/Q5");            
        }

        [Route("csrl23/q5/{id}")]
        [Route("csrl23/{id}/q5")]
        public IActionResult Q5(string id)
        {
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(5, survey, StaticReferralHelper.GetSentimentList());
            return View(vm);
        }
        [Route("csrl23/q5/{id}")]
        [Route("csrl23/{id}/q5")]
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
                ModelState.TryAddModelError("QuestionAresponse", "Your response is too long");
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
        [Route("csrl23/q6/{id}")]
        [Route("csrl23/{id}/q6")]
        public IActionResult Q6(string id)
        {
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(6, survey, StaticReferralHelper.GetYNList().Where(w => w.Key != "null"));
            return View(vm);
        }
        [Route("csrl23/q6/{id}")]
        [Route("csrl23/{id}/q6")]
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
        [Route("csrl23/q7/{id}")]
        [Route("csrl23/{id}/q7")]
        public IActionResult Q7(string id)
        {
            var survey = _sessionData.GetSessionData();            
            if (survey.NotificationKey == string.Empty && id != string.Empty)
                return Redirect("/u/" + id + "/sl");

            var vm = StaticQuestionnaireHelper.RetrieveAnswers(7, survey, StaticReferralHelper.GetYNList().Where(w => w.Key != "null"));
            return View(vm);
        }
        [Route("csrl23/q7/{id}")]
        [Route("csrl23/{id}/q7")]
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
