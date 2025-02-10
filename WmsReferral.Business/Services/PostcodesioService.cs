using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsReferral.Business.Services
{
    public static class PostcodesioServiceExtensions
    {
        public static void AddPostcodesioService(this IServiceCollection services, IConfiguration configuration)
        {        
            services.AddHttpClient<IPostcodesioService, PostcodesioService>()
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()                
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        //log warning                    
                        services.GetService<ILogger<PostcodesioService>>()?
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
    public class PostcodesioService : IPostcodesioService
    {
        private readonly HttpClient _httpClient;       

        public PostcodesioService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;            
        }
       
        public async Task<bool> ValidPostCodeAsync(string postcode)
        {
            
            var response = await this._httpClient.GetAsync($"https://api.postcodes.io/postcodes/{ postcode}/validate");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();                
                JObject jObj = JObject.Parse(content);
                return jObj.SelectToken("$.result").Value<bool>();                               
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<PostCodesioModel> LookupPostCodeAsync(string postcode)
        {
           
            var response = await this._httpClient.GetAsync($"https://api.postcodes.io/postcodes/{ postcode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                                    
                PostCodesioModel postcodeLookup = JsonConvert.DeserializeObject<PostCodesioModel>(content);

                return postcodeLookup;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

       
    }
}
