using IdentityModel.Client;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Services
{
    public static class NhsLoginServiceExtensions
    {
        public static void AddNhsLoginService(this IServiceCollection services)
        {
            services.AddHttpClient<INhsLoginService, NhsLoginService>()
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    //log warning                    
                    services.GetService<ILogger<NhsLoginService>>()?
                    .LogWarning(outcome.Exception, "Delaying for {delay}ms, then making retry {retry}.",
                    timespan.TotalMilliseconds,
                    retryAttempt);
                }));
        }

    }
    public class NhsLoginService : INhsLoginService
    {
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetry;
        private readonly string _NhsLoginUrl = string.Empty;

        public NhsLoginService(HttpClient httpClient, TelemetryClient telemetry, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _telemetry = telemetry;
            _NhsLoginUrl = configuration["WmsSelfReferral:NHSLoginUrl"];
        }

        public async Task<UserInfoResponse> GetUserInfo(string accessToken)
        {
            //var disco = await client.GetDiscoveryDocumentAsync(_NhsLoginUrl);
            var address = $"{_NhsLoginUrl}userinfo";
            var response = await _httpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = address,
                Token = accessToken
            });

            if (response.IsError)
            {
                _telemetry.TrackException(response.Exception);                
            }

            return response;
        }
    }
}
