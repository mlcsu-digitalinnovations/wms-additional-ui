using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Extensions.Http;
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
                .AddPolicyHandler(GetRetryPolicy());
        }
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            //5 retries, backing off by 2 seconds each time
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
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

        public async Task<IEnumerable<PostCodesioModel>> LookupPostCodeAsync(string postcode)
        {
           
            var response = await this._httpClient.GetAsync($"https://api.postcodes.io/postcodes/{ postcode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                IEnumerable<PostCodesioModel> postcodeLookup = JsonConvert.DeserializeObject<IEnumerable<PostCodesioModel>>(content);

                return postcodeLookup;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

       
    }
}
