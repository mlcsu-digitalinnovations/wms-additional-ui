using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using Microsoft.AspNetCore.Authentication;
using WmsSelfReferral.Helpers;

namespace WmsSelfReferral.Data
{
    public interface IReferralSessionData
    {
        SelfReferral GetReferralSessionData();
        KeyAnswer GetAnswerSessionData();
        void SetAnswerSessionData(KeyAnswer vm);
        void SetReferralSessionData(SelfReferral referral);
        bool ReferralSubmitted();
        bool ReferralCompleted();
        void SetProviderChoiceSessionData(ProviderChoiceModel model);
        ProviderChoiceModel GetProviderChoiceSessionData();
        Task<bool> NotValidSession();

    }
    public class ReferralSessionData : IReferralSessionData
    {
        private const string SESSION_KEY_SELFREFERRAL_TOKEN = "SelfReferral";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private const string SESSION_PROVIDER_CHOICE_TOKEN = "ProviderChoice";        
        private readonly ILogger<ReferralSessionData> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWmsReferralService _WmsService;
        public ReferralSessionData(ILogger<ReferralSessionData> logger, TelemetryClient telemetry,
            IHttpContextAccessor httpContextAccessor, IWmsReferralService wmsReferralService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _httpContextAccessor = httpContextAccessor;
            _WmsService = wmsReferralService;
        }

        public async Task<bool> NotValidSession()
        {
            var selfReferral = GetReferralSessionData();
            if (selfReferral.ConsentForGpAndNhsNumberLookup == null)
            {
                //error, redirect
                _telemetry.TrackEvent("GoneWrong:Inactivity");
                await _httpContextAccessor.HttpContext.SignOutAsync();
                //await HttpContext.ChallengeAsync();
                return true;
            }
            return false;
        }
        public SelfReferral GetReferralSessionData()
        {
            try
            {
                var srSession = _httpContextAccessor.HttpContext.Session.Get<SelfReferral>(SESSION_KEY_SELFREFERRAL_TOKEN);
                if (srSession == null) //if its null midway may need to start again
                    return new SelfReferral { };
                return srSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            //error
            return new SelfReferral { };
        }

        public void SetReferralSessionData(SelfReferral selfReferral)
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Set<SelfReferral>(SESSION_KEY_SELFREFERRAL_TOKEN, selfReferral);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public KeyAnswer GetAnswerSessionData()
        {
            try
            {
                var srSession = _httpContextAccessor.HttpContext.Session.Get<KeyAnswer>(SESSION_KEY_ANSWER_TOKEN);
                if (srSession == null) //if its null midway may need to start again
                    return new KeyAnswer
                    {
                        AnsweredDiabetesType1 = false,
                        AnsweredDiabetesType2 = false,
                        AnsweredHypertension = false,
                        AnsweredLearningDisability = false,
                        AnsweredPhysicalDisability = false,
                        ReferralSubmitted = false,
                        ProviderChoiceSubmitted = false,
                        AnsweredBariatricSurgery = false,
                        AnsweredBreastFeeding = false,
                        AnsweredCaesareanSection = false,
                        AnsweredEatingDisorder = false,
                        AnsweredGivenBirth = false,
                        AnsweredPregnant = false,
                        AnsweredNhsNumberGPConsent = false,
                        AnsweredUpdateReferrerCompletionConsent = false
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
        public void SetAnswerSessionData(KeyAnswer keyAnswer)
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Set<KeyAnswer>(SESSION_KEY_ANSWER_TOKEN, keyAnswer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
        public ProviderChoiceModel GetProviderChoiceSessionData()
        {
            try
            {
                var srSession = _httpContextAccessor.HttpContext.Session.Get<ProviderChoiceModel>(SESSION_PROVIDER_CHOICE_TOKEN);
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
        public void SetProviderChoiceSessionData(ProviderChoiceModel model)
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Set<ProviderChoiceModel>(SESSION_PROVIDER_CHOICE_TOKEN, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
        public bool ReferralCompleted()
        {
            var answers = GetAnswerSessionData();
            if (answers.AnsweredDiabetesType1 == true
                && answers.AnsweredDiabetesType2 == true
                && answers.AnsweredHypertension == true
                && answers.AnsweredLearningDisability == true
                && answers.AnsweredPhysicalDisability == true
                && answers.AnsweredConsentForFurtureContact == true
                && answers.AnsweredBariatricSurgery == true
                && answers.AnsweredEatingDisorder == true
                //&& answers.AnsweredPregnant == true
                )
            {
                return true;
            }

            return false;
        }
        public bool ReferralSubmitted()
        {
            var answers = GetAnswerSessionData();
            if (answers.ReferralSubmitted && answers.ProviderChoiceSubmitted)
            {
                return true;
            }

            return false;
        }

    }
}
