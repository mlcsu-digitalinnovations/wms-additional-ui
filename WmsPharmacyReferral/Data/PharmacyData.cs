using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using WmsPharmacyReferral.Helpers;
using WmsPharmacyReferral.Models;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;

namespace WmsPharmacyReferral.Data
{
    public class PharmacyData : IPharmacyData
    {
        private const string SESSION_KEY_PHARMACYREFERRAL_TOKEN = "PharmacyReferral";
        private const string SESSION_KEY_EMAIL = "AuthEmail";
        private const string SESSION_KEY_ANSWER_TOKEN = "Answers";
        private readonly ILogger<PharmacyData> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWmsReferralService _WmsService;
        public PharmacyData(ILogger<PharmacyData> logger, TelemetryClient telemetry,
            IHttpContextAccessor httpContextAccessor, IWmsReferralService wmsReferralService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _httpContextAccessor = httpContextAccessor;
            _WmsService = wmsReferralService;
        }

        public PharmacyReferral GetReferralSessionData()
        {
            try
            {
                var srSession = _httpContextAccessor.HttpContext.Session.Get<PharmacyReferral>(SESSION_KEY_PHARMACYREFERRAL_TOKEN);
                if (srSession == null)
                { //if its null midway may need to start again
                    var authSession = _httpContextAccessor.HttpContext.Session.Get<AuthViewModel>(SESSION_KEY_EMAIL);
                    if (authSession == null)
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
                _telemetry.TrackException(ex);
            }

            //error
            return new PharmacyReferral { };
        }

        public void SetReferralSessionData(PharmacyReferral referral)
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Set<PharmacyReferral>(SESSION_KEY_PHARMACYREFERRAL_TOKEN, referral);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackException(ex);
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
                        AnsweredUpdateReferrerCompletionConsent = false,
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
                _telemetry.TrackException(ex);
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
                _telemetry.TrackException(ex);
            }

        }
    }


    public interface IPharmacyData
    {     
        PharmacyReferral GetReferralSessionData();
        KeyAnswer GetAnswerSessionData();
        void SetAnswerSessionData(KeyAnswer vm);
        void SetReferralSessionData(PharmacyReferral referral);

    }
}
