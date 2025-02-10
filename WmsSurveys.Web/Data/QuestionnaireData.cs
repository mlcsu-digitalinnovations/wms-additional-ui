using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsSurveys.Web.Controllers;
using WmsSurveys.Web.Helpers;

namespace WmsSurveys.Web.Data
{
    public class QuestionnaireData : IQuestionnaireData
    {
        private const string SESSION_KEY_SURVEY = "SURVEY";
        private readonly ILogger<QuestionnaireData> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWmsReferralService _WmsService;
        public QuestionnaireData(ILogger<QuestionnaireData> logger, TelemetryClient telemetry,
            IHttpContextAccessor httpContextAccessor, IWmsReferralService wmsReferralService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _httpContextAccessor = httpContextAccessor;
            _WmsService = wmsReferralService;
        }

        public QuestionnaireViewModel GetSessionData()
        {
            try
            {
                var srSession = _httpContextAccessor?.HttpContext?.Session.Get<QuestionnaireViewModel>(SESSION_KEY_SURVEY);
                if (srSession == null) //if its null midway may need to start again
                    return new QuestionnaireViewModel
                    {
                        QuestionAnswers = new List<QuestionsViewModel>()
                    };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackTrace(ex.Message);
            }

            //error
            return new QuestionnaireViewModel { QuestionAnswers = new List<QuestionsViewModel>() };
        }

        public void SetSessionData(QuestionnaireViewModel vm)
        {
            try
            {
                _httpContextAccessor?.HttpContext?.Session.Set<QuestionnaireViewModel>(SESSION_KEY_SURVEY, vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackTrace(ex.Message);
            }

        }

        public void EndSession()
        {
            _httpContextAccessor?.HttpContext?.Session.Remove(SESSION_KEY_SURVEY);
        }


        public async Task<QuestionnaireViewModel> GetQuestionnaire(string key)
        {
            //get the users survey 
            var questionnaire = GetSessionData();
            if (questionnaire.NotificationKey != key)
            {
                var request = await _WmsService.GetQuestionnaireAsync(key);
                var json = await request.Content.ReadAsStringAsync();

                if (request.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrEmpty(json))
                    {
                        var jObj = JObject.Parse(json);
                        questionnaire.NotificationKey = key;
                        questionnaire.GivenName = jObj.SelectToken("$.givenName")?.Value<string>();
                        questionnaire.FamilyName = jObj.SelectToken("$.familyName")?.Value<string>();
                        questionnaire.FullName = questionnaire.GivenName + " " + questionnaire.FamilyName;
                        questionnaire.ValidationState = QuestionnaireValidationState.Valid;
                        questionnaire.Status = QuestionnaireStatus.Started;
                        questionnaire.QuestionAnswers = new List<QuestionsViewModel>();
                        questionnaire.QuestionnaireRequested = StaticQuestionnaireHelper.ConvertQuestionnaireType(jObj.SelectToken("$.questionnaireType")?.Value<string>() ?? "");
                        questionnaire.QuestionnaireType = (QuestionnaireType)Enum.Parse(typeof(QuestionnaireType), jObj.SelectToken("$.questionnaireType")?.Value<string>() ?? "NotSet");
                        questionnaire.ProviderName = jObj.SelectToken("$.providerName")?.Value<string>();
                        SetSessionData(questionnaire);
                    }
                    else
                    {
                        questionnaire.NotificationKey = key;
                        questionnaire.ValidationState = QuestionnaireValidationState.BadRequest;
                        questionnaire.Status = QuestionnaireStatus.TemporaryFailure;
                        SetSessionData(questionnaire);
                    }


                }
                else //not successful
                {
                    if (request.StatusCode == HttpStatusCode.BadRequest || request.StatusCode == HttpStatusCode.Conflict)
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            var jObj = JObject.Parse(json);
                            if (request.StatusCode == HttpStatusCode.Conflict)
                            {
                                var validationState = (QuestionnaireValidationState)Enum.Parse(typeof(QuestionnaireValidationState), jObj.SelectToken("$.type")?.Value<string>() ?? "TechnicalFailure");
                                questionnaire.ValidationState = validationState;
                                questionnaire.Status = QuestionnaireStatus.TechnicalFailure;

                            }

                            var errors = StaticReferralHelper.GetAPIErrorDictionary(json);
                            if (errors != null)
                            {
                                errors.Add("userKey", key);
                                _telemetry.TrackTrace("APIError: ", errors);
                            }
                        }
                        else
                        {
                            questionnaire.ValidationState = QuestionnaireValidationState.BadRequest;
                            questionnaire.Status = QuestionnaireStatus.TemporaryFailure;

                            _telemetry.TrackTrace("APIError: ", new Dictionary<string, string>() { { "API", "NoContent" } });
                        }
                        questionnaire.NotificationKey = key;
                        SetSessionData(questionnaire);

                    }
                    else if (request.StatusCode == HttpStatusCode.NotFound)
                    {
                        var validationState = QuestionnaireValidationState.NotificationKeyNotFound;
                        var status = QuestionnaireStatus.TechnicalFailure;

                        if (!string.IsNullOrEmpty(json))
                        {
                            var errors = StaticReferralHelper.GetAPIErrorDictionary(json);
                            if (errors != null)
                            {
                                errors.Add("userKey", key);
                                _telemetry.TrackTrace("Key not found: ", errors);
                            }
                        }                            

                        questionnaire.NotificationKey = key;
                        questionnaire.ValidationState = validationState;
                        questionnaire.Status = status;
                        SetSessionData(questionnaire);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            var jObj = JObject.Parse(json);

                            var validationState = jObj.SelectToken("$.validationState")?.Value<QuestionnaireValidationState>() ?? QuestionnaireValidationState.BadRequest;
                            var status = jObj.SelectToken("$.status")?.Value<QuestionnaireStatus>() ?? QuestionnaireStatus.TechnicalFailure;

                            var errors = StaticReferralHelper.GetAPIErrorDictionary(json);
                            if (errors != null)
                            {
                                errors.Add("userKey", key);
                                _telemetry.TrackTrace("Key not valid: ", errors);
                            }

                            questionnaire.NotificationKey = key;
                            questionnaire.ValidationState = validationState;
                            questionnaire.Status = status;
                        } else
                        {
                            questionnaire.NotificationKey = key;
                            questionnaire.ValidationState = QuestionnaireValidationState.BadRequest;
                            questionnaire.Status = QuestionnaireStatus.TemporaryFailure;
                        }
                            
                        SetSessionData(questionnaire);


                    }
                }



            }

            return questionnaire;
        }

        public async Task<QuestionnaireViewModel> CompleteQuestionnaire(string key)
        {
            var questionnaire = GetSessionData();
            //simple check, answers is greater than 5
            if (questionnaire.QuestionAnswers.Count > 5)
            {
                //clean some of the responses for the api
                //var apiQuestionnaire = questionnaire;
                var apiQuestionnaire = StaticQuestionnaireHelper.FormatforApi(questionnaire);

                var outnative = JsonConvert.SerializeObject(
                    apiQuestionnaire.QuestionAnswers
                    .OrderBy(o => o.QuestionId), Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        DefaultValueHandling = DefaultValueHandling.Include
                    });

                //submit questionnaire to API
                var request = await _WmsService.CompleteQuestionnaireAsync(
                    questionnaire,
                    outnative,
                    StaticQuestionnaireHelper.ConvertQuestionnaireType(questionnaire.QuestionnaireRequested)
                    );

                //success
                if (request.IsSuccessStatusCode)
                    questionnaire.Status = QuestionnaireStatus.Completed;

                //fail
                if (!request.IsSuccessStatusCode)
                {
                    //mark questionnaire as failed
                    questionnaire.Status = QuestionnaireStatus.TechnicalFailure;

                    if (request.StatusCode == HttpStatusCode.NotFound)//???
                        questionnaire.ValidationState = QuestionnaireValidationState.NotificationKeyNotFound;

                    if (request.StatusCode == HttpStatusCode.BadRequest)
                        questionnaire.ValidationState = QuestionnaireValidationState.BadRequest;

                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await request.Content.ReadAsStringAsync());
                    if (errors != null)
                    {
                        errors.Add("userKey", key);
                        _telemetry.TrackTrace("APIError: ", errors);

                        //add errors to questionnaire
                        questionnaire.ApiErrors = errors;
                    }
                    SetSessionData(questionnaire);
                }
            }
            else
            {
                questionnaire.Status = QuestionnaireStatus.TechnicalFailure;
                questionnaire.ValidationState = QuestionnaireValidationState.BadRequest;
            }
            return questionnaire;
        }







    }


    public interface IQuestionnaireData
    {
        QuestionnaireViewModel GetSessionData();
        void EndSession();
        void SetSessionData(QuestionnaireViewModel vm);
        Task<QuestionnaireViewModel> CompleteQuestionnaire(string key);
        Task<QuestionnaireViewModel> GetQuestionnaire(string key);

    }
}
