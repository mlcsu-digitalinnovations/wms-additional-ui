using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsSurveys.Web.Data;
using WmsSurveys.Web.Helpers;

namespace WmsSurveys.Web.Controllers
{
    public class QuestionnaireController : SessionControllerBase
    {
        private readonly ILogger<QuestionnaireController> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IWmsReferralService _WmsService;
        private readonly IQuestionnaireData _questionnaireData;

        public QuestionnaireController(ILogger<QuestionnaireController> logger, IWmsReferralService wmsReferralService,
             TelemetryClient telemetry, IQuestionnaireData questionnaireData)
        {
            _logger = logger;
            _WmsService = wmsReferralService;
            _telemetry = telemetry;
            _questionnaireData = questionnaireData;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Route("u/{id}")]
        [Route("u/")]
        public async Task<IActionResult> QuestionnaireRequest(string id)
        {
            //check id
            if (string.IsNullOrEmpty(id))
                return Redirect("/");

            //ensure 13, only take first 25 chars
            id = new string(id.Take(25).ToArray());

            //filter characters
            id = StaticReferralHelper.StringCleaner(id);

            //accept email link from api            
            var questionnaire = await _questionnaireData.GetQuestionnaire(id);

            //check status
            if (questionnaire.Status != QuestionnaireStatus.Started)
            {
                if (questionnaire.ValidationState == QuestionnaireValidationState.Expired)
                    return Redirect("/u/" + id + "/expired");
                if (questionnaire.ValidationState == QuestionnaireValidationState.NotificationKeyNotFound)
                    return Redirect("/u/" + id + "/notfound");
                if (questionnaire.ValidationState == QuestionnaireValidationState.Completed)
                    return Redirect("/u/" + id + "/completed");
                return Redirect("/u/" + id + "/error");
            }
            else
            {
                if (questionnaire.QuestionnaireRequested == "")
                    return Redirect("/u/" + id + "/error");
                return Redirect("/" + questionnaire.QuestionnaireRequested + "/" + id + "/begin");
            }
        }

        [Route("u/{id}/error")]
        [Route("u/error/{id}")]
        public IActionResult Problem(string id)
        {
            _telemetry.TrackEvent("Error questionnaire", new Dictionary<string, string>
            {
                { "UserRef", id },
                { "TraceId", HttpContext.TraceIdentifier }
            });

            var questionnaire = _questionnaireData.GetSessionData();
            
            //end session
            //_sessionData.EndSession();

            return View(questionnaire);
        }

        [Route("u/{id}/notfound")]
        [Route("u/notfound/{id}")]
        public IActionResult NotFound(string id)
        {
            _telemetry.TrackEvent("NotFound questionnaire", new Dictionary<string, string>
            {
                { "UserRef", id },
                { "TraceId", HttpContext.TraceIdentifier }
            });

            var questionnaire = _questionnaireData.GetSessionData();

            //end session
            _questionnaireData.EndSession();

            return View(questionnaire);
        }

        [Route("u/{id}/expired")]
        [Route("/expired")]
        public IActionResult Expired(string id)
        {
            _telemetry.TrackEvent("Expired questionnaire", new Dictionary<string, string>
            {
                { "UserRef", id },
                { "TraceId", HttpContext.TraceIdentifier }
            });

            var questionnaire = _questionnaireData.GetSessionData();

            //end session
            _questionnaireData.EndSession();

            return View(questionnaire);
        }

        [Route("u/{id}/completed")]
        [Route("/completed")]
        public IActionResult Completed(string id)
        {
            _telemetry.TrackEvent("Completed questionnaire", new Dictionary<string, string>
            {
                { "UserRef", id },
                { "TraceId", HttpContext.TraceIdentifier }
            });

            var questionnaire = _questionnaireData.GetSessionData();

            //end session
            _questionnaireData.EndSession();

            return View(questionnaire);
        }

        [Route("u/{id}/sl")]
        public IActionResult SessionLost(string id)
        {
            //track session lost
            _telemetry.TrackEvent("Session-Lost", new Dictionary<string, string>
            {
                { "UserRef", id },
                { "TraceId", HttpContext.TraceIdentifier }
            });

            //get the users survey again and restart
            var survey = _questionnaireData.GetSessionData();
            
            //ensure 13, only take first 25 chars
            id = new string(id.Take(25).ToArray());            
            if (survey.NotificationKey == string.Empty)
                survey.NotificationKey = id;
            return View(survey);
        }

       

        [Route("u/{id}/complete")]
        public async Task<IActionResult> Complete(string id)
        {
            if (id == string.Empty)
                Redirect("/");

            var questionnaire = await _questionnaireData.CompleteQuestionnaire(id);
            
            //incomplete
            if (questionnaire.Status == QuestionnaireStatus.Started)
                return Redirect("/" + questionnaire.QuestionnaireRequested + "/" + questionnaire.NotificationKey + "/begin");
            //something wrong
            if (questionnaire.Status == QuestionnaireStatus.TechnicalFailure)
                return Redirect("/u/" + questionnaire.NotificationKey + "/error");
            //success
            if (questionnaire.Status == QuestionnaireStatus.Completed)
                _questionnaireData.EndSession();

            return View();
        }

        [Route("/Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
        [Route("/Cookies")]
        public IActionResult Cookies()
        {
            return View();
        }
        [Route("/ContactUs")]
        public IActionResult ContactUs()
        {
            return View();
        }
        [Route("/Accessibility")]
        public IActionResult Accessibility()
        {
            return View();
        }
        [Route("/TermsAndConditions")]
        public IActionResult TermsAndConditions()
        {
            return View();
        }
        [Route("/Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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