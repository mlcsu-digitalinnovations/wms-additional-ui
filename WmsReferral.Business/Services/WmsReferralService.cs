using IdentityModel;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Services
{
    public static class WmsReferralServiceExtensions
    {
        public static void AddWmsReferralService(this IServiceCollection services)
        {
            services.AddHttpClient<IWmsReferralService, WmsReferralService>()
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                    .Or<TimeoutRejectedException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        //log warning                    
                        services.GetService<ILogger<WmsReferralService>>()?
                        .LogWarning(outcome.Exception, "Delaying for {delay}ms, then making retry {retry}.",
                        timespan.TotalMilliseconds,
                        retryAttempt);
                        
                    }))
                .AddPolicyHandler((services, request) => Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMilliseconds(6000),
                    TimeoutStrategy.Optimistic,
                    (context, timeout, _, exception) =>
                    {                        
                        services.GetService<ILogger<WmsReferralService>>()?
                            .LogWarning(exception, "Connection timed out after {timeout} seconds.",
                            timeout.TotalSeconds
                            );
                        return Task.CompletedTask;
                    })
                );
            
        }

    }
    public class WmsReferralService : IWmsReferralService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ReferralAPIBaseAddress = string.Empty;
        private readonly string _ReferralAPIKey = string.Empty;
        private readonly string _ReferralEndPoint = string.Empty;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<WmsReferralService> _logger;

        public WmsReferralService(HttpClient httpClient, IConfiguration configuration, TelemetryClient telemetry, ILogger<WmsReferralService> logger)
        {
            _httpClient = httpClient;
            _ReferralAPIBaseAddress = configuration["WmsReferral:apiBaseAddress"];
            _ReferralAPIKey = configuration["WmsReferral:apiKey"];
            _ReferralEndPoint = configuration["WmsReferral:apiEndPoint"];
            _telemetry = telemetry;
            _logger = logger;
        }
        public async Task<HttpResponseMessage> AddStaffReferralAsync(StaffReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/StaffReferral", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> AddSelfReferralAsync(SelfReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/GeneralReferral", jsoncontent);

            return response;
        }
        public async Task<HttpResponseMessage> UpdateSelfReferralAsync(SelfReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PutAsync($"{_ReferralAPIBaseAddress}/GeneralReferral/{referral.Id}", jsoncontent);

            return response;
        }
        public async Task<HttpResponseMessage> CancelElectiveCareReferralAsync(SelfReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PutAsync($"{_ReferralAPIBaseAddress}/GeneralReferral/{referral.Id}/cancel", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> AddPharmacyReferralAsync(PharmacyReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/PharmacyReferral", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> AddMskReferralAsync(MskReferral referral)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(referral);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/MskReferral", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> UpdateProviderChoiceAsync(ProviderChoiceModel providerChoiceModel, string referralId = "")
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(new { providerId = providerChoiceModel.ProviderId, Id = providerChoiceModel.Id });
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            if (referralId != "")
            { //it's a generalreferral
                jsoncontent = new StringContent("", Encoding.UTF8, "application/json");
                return await this._httpClient.PutAsync($"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}/{referralId}/Provider/{providerChoiceModel.ProviderId}", jsoncontent);
            }
            else
            { //its a staffreferral
                return await this._httpClient.PutAsync($"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}", jsoncontent);
            }
        }

        public async Task<HttpResponseMessage> EmailInUseAsync(string email, string endpoint)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(new { email = email });
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/{endpoint}/EmailInUse", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> NhsNumberInUseAsync(string nhsnumber)
        {
            PrepareAuthenticatedClient();

            var jsonRequest = JsonConvert.SerializeObject(new { NhsNumber = nhsnumber });
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}/nhsNumberInUse", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> NhsNumberAsync(string nhsnumber)
        {
            PrepareAuthenticatedClient();

            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}/nhsNumber/{nhsnumber}");

            return response;
        }

        public async Task<NhsNumberCheck> NhsNumberCheckAsync(string nhsnumber)
        {
            PrepareAuthenticatedClient();
            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}/{nhsnumber}");

            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    SelfReferral referral = JsonConvert.DeserializeObject<SelfReferral>(content);

                    return new NhsNumberCheck() { Referral = referral, StatusCode = 200 };
                }
                else if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new NhsNumberCheck() { StatusCode = 204 };
                }
                else
                {
                    var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await response.Content.ReadAsStringAsync());
                    telemErrors.TryAdd("NHS Number", nhsnumber);
                    telemErrors.TryAdd("StatusCode", response.StatusCode.ToString());
                    _telemetry.TrackEvent("GoneWrong:NHSNumberIssue", telemErrors);

                    return new NhsNumberCheck() { Errors = telemErrors, StatusCode = (int)response.StatusCode };
                }

            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent("GoneWrong:API");
                return new NhsNumberCheck() { Errors = new Dictionary<string, string> { { "Error", "" }, { "TraceId", "" } }, StatusCode = 500 };
            }
        }


        private async Task<IEnumerable<Ethnicity>> GetEthnicitiesAsync(string endpoint)
        {
            PrepareAuthenticatedClient();

            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/{endpoint}/Ethnicity");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                IEnumerable<Ethnicity> ethnicities = JsonConvert.DeserializeObject<IEnumerable<Ethnicity>>(content);

                return ethnicities;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }
        public async Task<IEnumerable<KeyValuePair<string, string>>> GetEthnicityGroupList(string endpoint)
        {
            List<KeyValuePair<string, string>> ethnicityGroupList = new();

            // use a service to get a list of ethnicity codes
            try
            {
                IEnumerable<Ethnicity> businessModel =
                  await GetEthnicitiesAsync(endpoint);

                foreach (string groupName in businessModel
                    .OrderBy(o => o.GroupOrder)
                    .GroupBy(g => g.GroupName)
                    .Select(s => s.First().GroupName))
                {
                    ethnicityGroupList.Add(new KeyValuePair<string, string>(groupName, groupName));
                }
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }

            return ethnicityGroupList.ToList();
        }
        public async Task<IEnumerable<KeyValuePair<string, string>>> GetEthnicityMembersList(string groupName, string endpoint)
        {
            List<KeyValuePair<string, string>> ethnicityMemberList = new();

            // use a service to get a list of ethnicity codes
            try
            {
                IEnumerable<Ethnicity> businessModel =
                  await GetEthnicitiesAsync(endpoint);

                foreach (Ethnicity ethnicity in businessModel
                    .Where(w => w.GroupName == groupName)
                    .OrderBy(o => o.DisplayOrder))
                {
                    ethnicityMemberList.Add(new KeyValuePair<string, string>(ethnicity.DisplayName, ethnicity.DisplayName));
                }

            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }

            return ethnicityMemberList;
        }
        public async Task<IEnumerable<Ethnicity>> GetEthnicities(string endpoint)
        {
            List<Ethnicity> ethnicityMemberList = new List<Ethnicity>();

            // use a service to get a list of ethnicity codes
            try
            {
                IEnumerable<Ethnicity> businessModel =
                  await GetEthnicitiesAsync(endpoint);

                return businessModel;
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }

            return ethnicityMemberList;
        }
        public async Task<string> GetEthnicityGroup(string ethnicityName, string endpoint)
        {
            string ethnicityGroup = "";

            // use a service to derive group from ethnicity
            try
            {
                IEnumerable<Ethnicity> businessModel =
                  await GetEthnicitiesAsync(endpoint);

                ethnicityGroup = businessModel.Where(w => w.DisplayName == ethnicityName).Select(s => s.GroupName).First();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }

            return ethnicityGroup;
        }

        public async Task<IEnumerable<StaffRole>> GetStaffRolesAsync()
        {
            PrepareAuthenticatedClient();

            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/StaffReferral/StaffRole");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                IEnumerable<StaffRole> staffRoles = JsonConvert.DeserializeObject<IEnumerable<StaffRole>>(content);

                return staffRoles;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<HttpResponseMessage> GenerateOTPTokenAsync(string email, int expireMinutes, string controller)
        {
            PrepareAuthenticatedClient();
            var query = "email=" + email + "&expireMinutes=" + expireMinutes;
            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/{controller}/GenerateKey?{query}");

            return response;
        }
        public async Task<HttpResponseMessage> ValidateOTPTokenAsync(string email, string token, string controller)
        {
            PrepareAuthenticatedClient();
            var query = "email=" + email + "&keyCode=" + HttpUtility.UrlEncode(token);
            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/{controller}/ValidateKey?{query}");

            return response;
        }

        public async Task<IEnumerable<MskHub>> GetMskHubs()
        {
            PrepareAuthenticatedClient();

            var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/MskReferral/MskHub ");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                IEnumerable<MskHub> mskHubs = JsonConvert.DeserializeObject<IEnumerable<MskHub>>(content);

                return mskHubs;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<HttpResponseMessage> GetQuestionnaireAsync(string key)
        {
            PrepareAuthenticatedClient();
            var jsonRequest = JsonConvert.SerializeObject(new { notificationKey = key });
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/Questionnaire/start", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> CompleteQuestionnaireAsync(QuestionnaireViewModel questionnaire, string answers, string questionnaireType)
        {
            PrepareAuthenticatedClient();
            var jsonRequest = JsonConvert.SerializeObject(new
            {
                notificationKey = questionnaire.NotificationKey,
                questionnaireType,
                answers
            });

            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/Questionnaire/complete", jsoncontent);

            return response;
        }

        public async Task<HttpResponseMessage> UploadElectiveCareFileAsync(Stream file, string fileExtension, string userid, string odsCode)
        {
            PrepareAuthenticatedClient();

            file.Seek(0, SeekOrigin.Begin);

            using var content = new MultipartFormDataContent
            {
                { new StreamContent(file), "file", "postedFile"+fileExtension },
                { new StringContent(userid), "TrustUserId" },
                { new StringContent(odsCode), "TrustOdsCode" }
            };
            content.Headers.ContentType.MediaType = "multipart/form-data";

            var response = await this._httpClient.PostAsync($"{_ReferralAPIBaseAddress}/electivecarereferral", content);

            return response;

        }

        public async Task<ElectiveCareQuota> ElectiveCareQuota(string odsCode)
        {
            PrepareAuthenticatedClient();
            try
            {
                var response = await this._httpClient.GetAsync($"{_ReferralAPIBaseAddress}/ElectiveCareReferral/{odsCode}/quota");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    ElectiveCareQuotaResult quota = JsonConvert.DeserializeObject<ElectiveCareQuotaResult>(content);
                    return new ElectiveCareQuota()
                    {
                        Status = response.StatusCode,
                        Result = quota
                    };
                }
                else
                {
                    //something went wrong                    
                    var errors = StaticReferralHelper.GetAPIErrorDictionary(await response.Content.ReadAsStringAsync());
                    _telemetry.TrackEvent("GoneWrong:ElectiveCare, quota", errors);

                    return new ElectiveCareQuota()
                    {
                        Status = response.StatusCode,
                        Errors = errors
                    };
                }

            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _telemetry.TrackEvent("GoneWrong:ElectiveCare, quota");
            }



            return new ElectiveCareQuota() { Status = HttpStatusCode.InternalServerError };
        }

        public async Task<bool> ValidateLinkId(string linkId)
        {
          bool linkIdIsValid = false;
          PrepareAuthenticatedClient();
          try
          {
            string url = $"{_ReferralAPIBaseAddress}/{_ReferralEndPoint}/ValidateLinkId?linkId={linkId}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
             linkIdIsValid = true;
            }
            else
            { 
              Dictionary<string, string> errors = new()
              {
                { "LinkId", linkId },
                { "StatusCode", response.StatusCode.ToString() }
              };
              _logger.LogError($"Error validating link ID {linkId}: {response.StatusCode}");
              _telemetry.TrackEvent("GoneWrong:ValidateLinkId", errors);
            }
          }
          catch (Exception ex)
          {
            _telemetry.TrackException(ex);
            _logger.LogError(ex.Message);
            _telemetry.TrackEvent("GoneWrong:API");
          }

          return linkIdIsValid;
        }

        private void PrepareAuthenticatedClient()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-API-KEY", _ReferralAPIKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
