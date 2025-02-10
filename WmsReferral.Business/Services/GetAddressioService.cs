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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsReferral.Business.Services
{
    public static class GetAddressioServiceExtensions
    {
        public static void AddGetAddressioService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IGetAddressioService, GetAddressioService>(client => 
            {
                client.BaseAddress = new Uri(configuration["WmsReferral:GetAddressIOapiBaseAddress"]);               
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            })
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()                
                    .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            //log warning                    
                            services.GetService<ILogger<GetAddressioService>>()?
                            .LogWarning(outcome.Exception, "Delaying for {delay}ms, then making retry {retry}.",
                            timespan.TotalMilliseconds,
                            retryAttempt);
                        }))
                .AddPolicyHandler((services, request) => Policy.TimeoutAsync<HttpResponseMessage>(
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
    public class GetAddressioService : IGetAddressioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly TelemetryClient _telemetry;

        public GetAddressioService(HttpClient httpClient, IConfiguration configuration, TelemetryClient telemetry)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WmsReferral:GetAddressIOapiKey"];
            _telemetry = telemetry;
        }

        public async Task<HttpResponseMessage> FindAddressAsync(string postcode)
        {  
            var response = await this._httpClient.GetAsync($"find/{postcode}?api-key={_apiKey}&sort=True");
            return response;
        }
        public async Task<IEnumerable<KeyValuePair<string, string>>> GetAddressList(string postcode)
        {
            List<KeyValuePair<string, string>> addressList = new();
            
            // use a service to get a list of addresses
            try
            {
                var response = await FindAddressAsync(postcode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    GetAddressioModel getAddressio = JsonConvert.DeserializeObject<GetAddressioModel>(content);

                    foreach (string address in getAddressio.Addresses)
                    {
                        addressList.Add(new KeyValuePair<string, string>(address, address.Split(",")[0]));
                    }

                    if (getAddressio.Addresses.Count == 0)
                    {
                        //no addresses??? should have been a 404
                        Dictionary<string, string> telemErrors = new()
                        {
                            { "Status Code", response.StatusCode.ToString() },
                            { "Error", "Got 200, empty address list"},
                            { "PostCode", postcode }
                        };
                        _telemetry.TrackEvent("GoneWrong:GetAddressIO", telemErrors);
                        addressList.Add(new KeyValuePair<string, string>("Error", "404"));
                    }

                } else
                {
                    Dictionary<string, string> telemErrors = new()
                    {
                        { "Status Code", response.StatusCode.ToString() },
                        { "Error", "" },
                        { "PostCode", postcode }
                    };
                    _telemetry.TrackEvent("GoneWrong:GetAddressIO", telemErrors);
                    addressList.Add(new KeyValuePair<string, string>("Error", response.StatusCode.ToString()));
                }

                
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                addressList.Add(new KeyValuePair<string, string>("Error", "Error"));
            }

            return addressList.ToList();
        }


    }
}
