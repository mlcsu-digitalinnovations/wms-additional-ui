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

namespace WmsSelfReferral.Helpers
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
        private readonly string _NhsLoginUrl;
        private readonly string _NhsLoginRsaKey;
        private readonly string _NhsLoginClientId;
        private readonly string _NhsLoginVectorsofTrust;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<CookieAuthenticationEventsHelper> _logger;
        public CookieAuthenticationEventsHelper(HttpClient httpClient, IConfiguration configuration, ILogger<CookieAuthenticationEventsHelper> logger, TelemetryClient telemetry)
        {
            _httpClient = httpClient;
            _NhsLoginUrl = configuration["WmsSelfReferral:NhsLoginUrl"];
            _NhsLoginRsaKey = configuration["WmsSelfReferral:NhsLoginRsaKey"];
            _NhsLoginClientId = configuration["WmsSelfReferral:NhsLoginClientId"];
            _NhsLoginVectorsofTrust = "[\"P5.Cp.Cd\", \"P5.Cp.Ck\", \"P5.Cm\"]";
            _logger = logger;
            _telemetry = telemetry;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            const string accessTokenName = "access_token";
            const string refreshTokenName = "refresh_token";
            const string expirationTokenName = "expires_at";

            if (context.Principal.Identity.IsAuthenticated)
            {
                var exp = context.Properties.GetTokenValue(expirationTokenName);
                if (exp != null)
                {
                    var expires = DateTime.Parse(exp, CultureInfo.InvariantCulture).ToUniversalTime();

                    if (expires < DateTime.UtcNow.AddMinutes(1))
                    {
                        // If we don't have the refresh token, then check if this client has set the
                        // "AllowOfflineAccess" property set in Identity Server and if we have requested
                        // the "OpenIdConnectScope.OfflineAccess" scope when requesting an access token.
                        var refreshToken = context.Properties.GetTokenValue(refreshTokenName);
                        if (refreshToken == null || refreshToken == "")
                        {
                            _logger.LogError("Refresh token is null/empty");
                            context.RejectPrincipal();
                            return;
                        }

                        var cancellationToken = context.HttpContext.RequestAborted;
                                                
                        // Set the token client options
                        var tokenClientOptions = new TokenClientOptions
                        {
                            Address = _NhsLoginUrl + "token",
                            ClientId = _NhsLoginClientId,
                            ClientAssertion = new ClientAssertion()
                            {
                                Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                                Value = TokenHelper.CreateClientAuthJwt(
                                    _NhsLoginUrl,
                                    _NhsLoginRsaKey,
                                    _NhsLoginClientId)
                            },

                        };
                        
                        //setup the client
                        var tokenClient = new TokenClient(_httpClient, tokenClientOptions);

                        //request new access token using refreshtoken
                        var tokenResponse = await tokenClient
                            .RequestRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        
                        //any error from endpoint then reject
                        if (tokenResponse.IsError)
                        {
                            _logger.LogError(tokenResponse.Exception, "Failed to refresh token");
                            _telemetry.TrackEvent("Failed to refresh token", new Dictionary<string, string>()
                                {
                                    { "tokenerror", tokenResponse.Error ?? "none recorded" },
                                    { "tokenerrorType", tokenResponse.ErrorType.ToString() },
                                    { "tokenerrorDescription",tokenResponse.ErrorDescription ?? "none recorded" }
                                });

                            
                            context.RejectPrincipal();
                            return;
                        }

                        //read successful token and validate contents
                        var handler = new JwtSecurityTokenHandler();
                        var accessToken = handler.ReadJwtToken(tokenResponse.AccessToken);

                        var vtrclaim = accessToken.Claims
                            .Where(w => w.Type == "vot")
                            .FirstOrDefault();

                        if (vtrclaim != null)
                        {
                            var subclaim = accessToken.Claims
                                .Where(w => w.Type == "sub")
                                .FirstOrDefault();
                            if (!_NhsLoginVectorsofTrust.Contains(vtrclaim.Value))
                            {
                                //if VOT doesnt match what we passed, possible mitm                   
                                _telemetry.TrackEvent("VOT incorrect", new Dictionary<string, string>()
                                {
                                    { "sub", subclaim.Value },
                                    { "claimRequested",vtrclaim.Value }
                                });

                                //log out
                                context.RejectPrincipal();
                                return;
                            }
                        }

                        // Update the tokens
                        var expirationValue = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o", CultureInfo.InvariantCulture);
                        context.Properties.StoreTokens(new[]
                        {
                            new AuthenticationToken { Name = refreshTokenName, Value = tokenResponse.RefreshToken ?? refreshToken },
                            new AuthenticationToken { Name = accessTokenName, Value = tokenResponse.AccessToken },
                            new AuthenticationToken { Name = expirationTokenName, Value = expirationValue }
                        });

                        // Update the cookie with the new tokens

                        context.ShouldRenew = true;
                    }
                }
            }
        }
    }
}
