using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsReferral.Business.Services
{
    public static class ODSLookupServiceExtensions
    {
        public static void AddODSLookupService(this IServiceCollection services)
        {
            services.AddHttpClient<IODSLookupService, ODSLookupService>()
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()                
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    //log warning                    
                    services.GetService<ILogger<ODSLookupService>>()?
                    .LogWarning(outcome.Exception, "Delaying for {delay}ms, then making retry {retry}.",
                    timespan.TotalMilliseconds,
                    retryAttempt);
                })).AddPolicyHandler((services, request) => Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMilliseconds(5000),
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
    public class ODSLookupService : IODSLookupService
    {

        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetry;

        public ODSLookupService(HttpClient httpClient, TelemetryClient telemetry)
        {
            _httpClient = httpClient;
            _telemetry = telemetry;
        }

        public async Task<ODSOrganisation> LookupODSCodeAsync(string ODSCode)
        {
            var response = await this._httpClient.GetAsync($"https://directory.spineservices.nhs.uk/ORD/2-0-0/organisations/{ ODSCode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                JObject jObj = JObject.Parse(content);

                var odsOrg = new ODSOrganisation
                {
                    APIStatusCode = 200,
                    Name = jObj.SelectToken("$.Organisation.Name").Value<string>(),
                    AddrLn1 = jObj.SelectToken("$.Organisation.GeoLoc.Location.AddrLn1")?.Value<string>(),
                    AddrLn2 = jObj.SelectToken("$.Organisation.GeoLoc.Location.AddrLn2")?.Value<string>(),
                    PostCode = jObj.SelectToken("$.Organisation.GeoLoc.Location.PostCode")?.Value<string>(),
                    County = jObj.SelectToken("$.Organisation.GeoLoc.Location.County")?.Value<string>(),
                    Country = jObj.SelectToken("$.Organisation.GeoLoc.Location.Country")?.Value<string>(),
                    Town = jObj.SelectToken("$.Organisation.GeoLoc.Location.Town")?.Value<string>(),
                    Status = jObj.SelectToken("$.Organisation.Status")?.Value<string>()
                };
                return odsOrg;
            }

            return new ODSOrganisation() { APIStatusCode = (int)response.StatusCode };
        }


    }
}
