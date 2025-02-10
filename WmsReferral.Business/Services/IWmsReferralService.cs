using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Services
{
    public interface IWmsReferralService
    {
        Task<HttpResponseMessage> AddStaffReferralAsync(StaffReferral referral);
        Task<HttpResponseMessage> UpdateProviderChoiceAsync(ProviderChoiceModel providerChoiceModel, string referralId="");        
        //Task<IEnumerable<Ethnicity>> GetEthnicitiesAsync(string endpoint);
        Task<IEnumerable<KeyValuePair<string, string>>> GetEthnicityGroupList(string endpoint);
        Task<IEnumerable<KeyValuePair<string, string>>> GetEthnicityMembersList(string groupName, string endpoint);
        Task<IEnumerable<Ethnicity>> GetEthnicities(string endpoint);
        Task<string> GetEthnicityGroup(string ethnicityName, string endpoint);
        Task<IEnumerable<StaffRole>> GetStaffRolesAsync();
        Task<HttpResponseMessage> EmailInUseAsync(string email, string endpoint);
        Task<HttpResponseMessage> NhsNumberInUseAsync(string email);
        Task<HttpResponseMessage> NhsNumberAsync(string nhsnumber);
        Task<HttpResponseMessage> GenerateOTPTokenAsync(string email, int expireMinutes, string controller);
        Task<HttpResponseMessage> ValidateOTPTokenAsync(string email, string token, string controller);
        Task<HttpResponseMessage> AddPharmacyReferralAsync(PharmacyReferral referral);
        Task<HttpResponseMessage> AddSelfReferralAsync(SelfReferral referral);
        Task<HttpResponseMessage> UpdateSelfReferralAsync(SelfReferral referral);
        Task<HttpResponseMessage> CancelElectiveCareReferralAsync(SelfReferral referral);
        Task<NhsNumberCheck> NhsNumberCheckAsync(string nhsnumber);
        Task<HttpResponseMessage> AddMskReferralAsync(MskReferral referral);
        Task<IEnumerable<MskHub>> GetMskHubs();
        Task<HttpResponseMessage> GetQuestionnaireAsync(string key);
        Task<HttpResponseMessage> CompleteQuestionnaireAsync(QuestionnaireViewModel questionnaire, string answers, string questionnaireType);
        Task<HttpResponseMessage> UploadElectiveCareFileAsync(Stream file, string fileExtension, string userid, string odsCode);
        Task<ElectiveCareQuota> ElectiveCareQuota(string odsCode);
        Task<bool> ValidateLinkId(string linkId);
    }
}

