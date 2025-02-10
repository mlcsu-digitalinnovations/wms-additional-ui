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
namespace WmsElectiveCarePortal.Helpers
{
    public class OpenIdEventsHelper : OpenIdConnectEvents
    {

        private readonly TelemetryClient _telemetry;
        private readonly ILogger<OpenIdEventsHelper> _logger;
        public OpenIdEventsHelper(IConfiguration configuration, ILogger<OpenIdEventsHelper> logger, TelemetryClient telemetry)
        {

            _logger = logger;
            _telemetry = telemetry;
        }
        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            var id_token_hint = context.Properties.Items.FirstOrDefault(x => x.Key == "tokenhint").Value;
            var policy = context.Properties.Items.FirstOrDefault(x => x.Key == "policy").Value;
            if (id_token_hint != null)
            {
                // Send parameter to authentication request
                context.ProtocolMessage.SetParameter("id_token_hint", id_token_hint);
            }
            if (policy != null)
            {
                // Send parameter to authentication request
                context.ProtocolMessage.SetParameter("p", policy);
            }
            return Task.CompletedTask;
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
                _telemetry.TrackEvent("Login Error", new Dictionary<string, string>()
                    {
                        { "Error", context.ProtocolMessage.Error },
                        { "Error Description",context.ProtocolMessage.ErrorDescription },
                        { "Trace", context.HttpContext.TraceIdentifier }

                    });


                if (context.ProtocolMessage.ErrorDescription.Contains("AADB2C90091"))
                {
                    //user cancelled                    
                    if (context.Properties != null)
                        if (context.Properties.Items.TryGetValue(".redirect", out string? redirectPath))
                        {
                            context.HandleResponse();
                            context.Response.Redirect(redirectPath??"/ElectiveCare");
                        }
                } else
                {
                    context.HandleResponse();
                    context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
                }
                
            }

            return Task.FromResult(0);
        }
        public override Task RemoteFailure(RemoteFailureContext context)
        {
            _telemetry.TrackEvent("Login Error", new Dictionary<string, string>()
            {
                { "Error", context.Failure.Message },
                { "Error Description", context.Failure.Message.Contains("Correlation failed") ? "User took too long" : "Unknown" },
                { "Trace", context.HttpContext.TraceIdentifier }

            });


            context.HandleResponse();
            context.Response.Redirect("/login-error?TraceId=" + context.HttpContext.TraceIdentifier);
            return Task.FromResult(0);
        }


    }
}
