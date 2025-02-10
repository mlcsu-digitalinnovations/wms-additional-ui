using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WmsPharmacyReferral.Models;
using WmsReferral.Business.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using WmsMskReferral.Models;
using System.Security.Principal;

namespace WmsReferral.Tests
{
    public static class TestSetup
    {

        public static TelemetryClient InitializeMockTelemetryChannel()
        {
            MockTelemetryChannel mockTelemetryChannel = new();
            TelemetryConfiguration mockTelemetryConfig = new()
            {
                TelemetryChannel = mockTelemetryChannel,
                //InstrumentationKey = Guid.NewGuid().ToString(),
            };

            TelemetryClient mockTelemetryClient = new(mockTelemetryConfig);
            return mockTelemetryClient;
        }
        public static IConfiguration InitializeConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"PharmacyReferral", "TokenExpiry"},
                {"PharmacyReferral:TokenExpiry", "30"},
                {"WmsSelfReferral:NhsLoginClientId","mlcsuappsdevwmp" },
                {"WmsSelfReferral:NhsLoginUrl","https://auth.sandpit.signin.nhs.uk/" }
                //...populate as needed for the test
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            return configuration;
        }
        public static HttpResponseMessage HttpResponseMessageMock(HttpStatusCode code, HttpContent content)
        {
            return new HttpResponseMessage() { StatusCode = code, Content = content };
        }
        public static Mock<HttpContext> InitializeAuthControllerContext(AuthViewModel authemail)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            var sessionAuth = JsonConvert.SerializeObject(authemail);
            byte[] srbytes = System.Text.Encoding.UTF8.GetBytes(sessionAuth);


            mockSession.Setup(x => x.TryGetValue("AuthEmail", out srbytes)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);

            return mockContext;
        }
        public static Mock<HttpContext> InitializePharmacyControllerContext(WmsReferral.Business.Models.PharmacyReferral referral, KeyAnswer keyAnswer)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            var pharmacyReferral = JsonConvert.SerializeObject(referral);
            byte[] srbytes = System.Text.Encoding.UTF8.GetBytes(pharmacyReferral);

            var sessionKeyAnswer = JsonConvert.SerializeObject(keyAnswer);
            byte[] skabytes = System.Text.Encoding.UTF8.GetBytes(sessionKeyAnswer);



            mockSession.Setup(x => x.TryGetValue("PharmacyReferral", out srbytes)).Returns(true);
            mockSession.Setup(x => x.TryGetValue("Answers", out skabytes)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);

            return mockContext;
        }
        public static Mock<HttpContext> InitializeControllerContext(Referral referral, KeyAnswer keyAnswer, ProviderChoiceModel providerChoiceModel, string referralType, AuthViewModel authViewModel = null, MskHubViewModel mskHubViewModel = null)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            var sessionReferral = JsonConvert.SerializeObject(referral);
            byte[] srbytes = System.Text.Encoding.UTF8.GetBytes(sessionReferral);

            var sessionKeyAnswer = JsonConvert.SerializeObject(keyAnswer);
            byte[] skabytes = System.Text.Encoding.UTF8.GetBytes(sessionKeyAnswer);

            var sessionProviderChoice = JsonConvert.SerializeObject(providerChoiceModel);
            byte[] spcbytes = System.Text.Encoding.UTF8.GetBytes(sessionProviderChoice);

            if (authViewModel!=null && referralType=="PharmacyReferral")
            {
                var sessionAuthView = JsonConvert.SerializeObject(authViewModel);
                byte[] authviewbytes = System.Text.Encoding.UTF8.GetBytes(sessionAuthView);
                mockSession.Setup(x => x.TryGetValue("AuthEmail", out authviewbytes)).Returns(true);
            }

            if (mskHubViewModel != null && referralType == "MskReferral")
            {
                var sessionMskHubView = JsonConvert.SerializeObject(mskHubViewModel);
                byte[] mskhubviewbytes = System.Text.Encoding.UTF8.GetBytes(sessionMskHubView);
                mockSession.Setup(x => x.TryGetValue("AuthEmail", out mskhubviewbytes)).Returns(true);
            }

            mockSession.Setup(x => x.TryGetValue(referralType, out srbytes)).Returns(true);
            mockSession.Setup(x => x.TryGetValue("Answers", out skabytes)).Returns(true);
            mockSession.Setup(x => x.TryGetValue("ProviderChoice", out spcbytes)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);


            //mock authentication
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(_ => _.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.FromResult((object)null));

            //mock token
            var authResult = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(), null));
            authResult.Properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = "accessTokenValue" }
            });
            authServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), null))
                .ReturnsAsync(authResult);



            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IUrlHelperFactory)))
                .Returns(urlHelperFactory.Object);

            

            mockContext.Setup(s => s.RequestServices).Returns(serviceProviderMock.Object);

            //mock user
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, "John Doe"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("name", "John Doe"),
            };
            var identity = new ClaimsIdentity(claims, "OpenId");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(x => x.Identity).Returns(identity);
            //mockPrincipal.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);

            mockContext.Setup(m => m.User).Returns(claimsPrincipal);



            return mockContext;
        }

        public static Mock<HttpContext> InitializeQuestionnaireContext(QuestionnaireViewModel questionnaire)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            var sessionQuestionnaire = JsonConvert.SerializeObject(questionnaire);
            byte[] qbytes = System.Text.Encoding.UTF8.GetBytes(sessionQuestionnaire);
            mockSession.Setup(x => x.TryGetValue("SURVEY", out qbytes)).Returns(true);

            mockContext.Setup(s => s.Session).Returns(mockSession.Object);
            return mockContext;
        }

        public static string ApiError()
        {
            return @"{
              'type': 'string',
              'title': 'string',
              'traceId' : 'string',
              'status': 0,
              'detail': 'string',
              'instance': 'string',
              'errors': {
                        'prop1': [ 'val1' ]
                        }
            }";
        }
    }

    public class MockTelemetryChannel : ITelemetryChannel
    {
        public ConcurrentBag<ITelemetry> SentTelemtries = new ConcurrentBag<ITelemetry>();
        public bool IsFlushed { get; private set; }
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            this.SentTelemtries.Add(item);
        }

        public void Flush()
        {
            this.IsFlushed = true;
        }

        public void Dispose()
        {

        }
    }
}
