using IdentityModel.Client;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WmsMskReferral.Helpers
{
    public static class OpenIdRefreshTokenServiceExtensions
    {
        public static void AddOpenIdRefreshTokenService(this IServiceCollection services)
        {
            services.AddHttpClient<CookieAuthenticationEventsHelper>()
                .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    //log warning                    
                    services.GetService<ILogger<CookieAuthenticationEventsHelper>>()?
                    .LogWarning(outcome.Exception, "Delaying for {delay}ms, then making retry {retry}.",
                    timespan.TotalMilliseconds,
                    retryAttempt);
                }));
        }

    }
    public class CookieAuthenticationEventsHelper : CookieAuthenticationEvents
    {
        private readonly HttpClient _httpClient;
        private readonly string _OAuthUrl;
        private readonly string _ClientSecret;
        private readonly string _ClientId;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<CookieAuthenticationEventsHelper> _logger;
        public CookieAuthenticationEventsHelper(HttpClient httpClient, IConfiguration configuration, ILogger<CookieAuthenticationEventsHelper> logger, TelemetryClient telemetry)
        {
            _httpClient = httpClient;
            _OAuthUrl = "https://fs.nhs.net/adfs/oauth2";
            _ClientSecret = configuration["WmsMskReferral:NhsMailSecret"];
            _ClientId = configuration["WmsMskReferral:NhsMailClientId"];
            _logger = logger;
            _telemetry = telemetry;
        }
        public override Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
        {
            return base.RedirectToLogout(context);
        }
        public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            return base.ValidatePrincipal(context);
        }
        
    }
}
