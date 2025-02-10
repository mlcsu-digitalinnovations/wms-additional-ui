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

namespace WmsMskReferral.Helpers
{
    public class OpenIdEventsHelper : OpenIdConnectEvents
    {
        private readonly string _OAuthUrl;
        private readonly string _ClientSecret;
        private readonly string _ClientId;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<OpenIdEventsHelper> _logger;
        public OpenIdEventsHelper(IConfiguration configuration, ILogger<OpenIdEventsHelper> logger, TelemetryClient telemetry)
        {
            _OAuthUrl = "https://fs.nhs.net/adfs/oauth2";
            _ClientSecret = configuration["WmsMskReferral:NhsMailSecret"];
            _ClientId = configuration["WmsMskReferral:NhsMailClientId"];
            _logger = logger;
            _telemetry = telemetry;
        }
        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            //set redirect uri to always be the welcome page
            string appBaseUrl = context.Request.Scheme + "://"
                                + context.Request.Host + context.Request.PathBase;

            context.ProtocolMessage.RedirectUri = appBaseUrl + "/signin-oidc";
            context.Properties.RedirectUri = appBaseUrl + "/MskHub/NHSMail-Landing";
            context.ProtocolMessage.ErrorUri = appBaseUrl + "/login-error";
            
            var login_hint = context.Properties.GetParameter<string>("login_hint");
            if (login_hint!=null)
                context.ProtocolMessage.LoginHint = login_hint.ToString();


            return Task.CompletedTask;
        }
        public override Task RemoteSignOut(RemoteSignOutContext context)
        {
            return base.RemoteSignOut(context);
        }
        public override Task RedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            return base.RedirectToIdentityProviderForSignOut(context);
        }
        public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            
            return Task.CompletedTask;
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
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
                _telemetry.TrackEvent("NHSMail Error", new Dictionary<string, string>()
                    {
                        { "Error", context.ProtocolMessage.Error },
                        { "Error Description",context.ProtocolMessage.ErrorDescription },
                        { "Trace", context.HttpContext.TraceIdentifier }

                    });

                _logger.LogTrace("NHSMail Error", new Dictionary<string, string>()
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
            //handle back button/correlation failure
            if (context.Failure != null)
            {
                if (context.Failure.Message == "Correlation failed.")
                {
                    context.HandleResponse();
                    context.Response.Redirect("/MskHub/select-msk-hub");
                }
                    
            } else
            {
                context.HandleResponse();
                context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
            }
            
            return Task.FromResult(0);
        }


    }
}
