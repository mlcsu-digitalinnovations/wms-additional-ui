using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Helpers
{
    public class OpenIdEventsHelper : OpenIdConnectEvents
    {
        private readonly string _NhsLoginUrl;
        private readonly string _NhsLoginRsaKey;
        private readonly string _NhsLoginClientId;
        private readonly string _NhsLoginVectorsofTrust;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<OpenIdEventsHelper> _logger;
        public OpenIdEventsHelper(IConfiguration configuration, ILogger<OpenIdEventsHelper> logger, TelemetryClient telemetry)
        {
            _NhsLoginUrl = configuration["WmsSelfReferral:NhsLoginUrl"];
            _NhsLoginRsaKey = configuration["WmsSelfReferral:NhsLoginRsaKey"];
            _NhsLoginClientId = configuration["WmsSelfReferral:NhsLoginClientId"];
            _NhsLoginVectorsofTrust = "[\"P5.Cp.Cd\", \"P5.Cp.Ck\", \"P5.Cm\"]";
            _logger = logger;
            _telemetry = telemetry;
        }
        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
            {
                //set vectors of trust
                context.ProtocolMessage.Parameters.Add("vtr", _NhsLoginVectorsofTrust);
                // <null>=sso if valid, none=sso only with valid session, login=user must alway login 
                context.ProtocolMessage.Parameters.Add("prompt", null);
            }

            //set redirect uri to always be the welcome page
            string appBaseUrl = context.Request.Scheme + "://"
                                + context.Request.Host + context.Request.PathBase;

            context.ProtocolMessage.RedirectUri = appBaseUrl + "/signin-oidc";
            context.Properties.RedirectUri = appBaseUrl + "/selfReferral";
            context.ProtocolMessage.ErrorUri = appBaseUrl + "/login-error";

            if (context.Request.Path.ToString().ToLower() != "/selfreferral/login" || context.Request.Path.ToString().ToLower() != "/selfreferral/register")
            {
                _telemetry.TrackEvent("RedirectToNHSLogin", new Dictionary<string, string>()
                {
                    { "path", context.Request.Path },
                    { "cookies" , String.Join(", ",context.Request.Cookies.Keys.Select(s=>s)) },
                    
                });
            }



            return Task.CompletedTask;
        }

        public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            if (context.TokenEndpointRequest?.GrantType == OpenIdConnectGrantTypes.AuthorizationCode)
            {
                context.TokenEndpointRequest.ClientAssertionType =
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                context.TokenEndpointRequest.ClientAssertion = TokenHelper.CreateClientAuthJwt(
                    _NhsLoginUrl,
                    _NhsLoginRsaKey,
                    _NhsLoginClientId
                    );
            }

            return Task.CompletedTask;
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
            //confirm vot returned matches our request
            var vtrclaim = context.SecurityToken.Claims
                .Where(w => w.Type == "vot")
                .FirstOrDefault();

            var subclaim = context.SecurityToken.Claims
                .Where(w => w.Type == "sub")
                .FirstOrDefault();

            if (vtrclaim != null)
            {
                if (!_NhsLoginVectorsofTrust.Contains(vtrclaim.Value))
                {
                    //if VOT doesnt match what we passed, possible mitm                   
                    _telemetry.TrackEvent("VOT incorrect", new Dictionary<string, string>()
                    {
                        { "sub", subclaim.Value },
                        { "claimRequested",vtrclaim.Value },
                        { "Trace", context.HttpContext.TraceIdentifier }

                    });
                    context.HandleResponse();
                    context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
                }

            }


            return Task.CompletedTask;
        }
        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {

            context.HandleResponse();
            context.Response.Redirect("/login-error");
            return Task.FromResult(0);
        }

        public override Task MessageReceived(MessageReceivedContext context)
        {
            if (context.ProtocolMessage.Error != null)
            {
                _telemetry.TrackEvent("NHS Login Error", new Dictionary<string, string>()
                    {
                        { "Error", context.ProtocolMessage.Error },
                        { "Error Description",context.ProtocolMessage.ErrorDescription },
                        { "Trace", context.HttpContext.TraceIdentifier }

                    });

                if (context.ProtocolMessage.Error == "access_denied"
                    && context.ProtocolMessage.ErrorDescription == "ConsentNotGiven")
                {
                    //if consent not given
                    context.HandleResponse();
                    context.Response.Redirect("/access-denied");

                    return Task.FromResult(0);
                }

                context.HandleResponse();
                context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
            }

            return Task.FromResult(0);
        }
        public override Task RemoteFailure(RemoteFailureContext context)
        {
            _telemetry.TrackEvent("NHS Login Error", new Dictionary<string, string>()
            {
                { "Error", context.Failure.Message },
                { "Error Description", context.Failure.Message.Contains("Correlation failed") ? "User took too long" : "Unknown" },
                { "Trace", context.HttpContext.TraceIdentifier }

            });

            _telemetry.TrackException(context.Failure);

            context.HandleResponse();

            //if corelation failed, likely user took too long to register, redirect to login, otherwise report error
            if (context.Failure.Message.Contains("Correlation failed"))
                context.Response.Redirect("/selfReferral/login");

            context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
            return Task.FromResult(0);
        }


    }
}
