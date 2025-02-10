using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business;
using WmsReferral.Business.Shared;
using WmsPharmacyReferral.Controllers;
using Microsoft.Extensions.Configuration;
using WmsPharmacyReferral.Models;
using WmsReferral.Tests;
namespace WmsReferral.Tests.PharmacyReferral.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthController _classUnderTest;
        private readonly Mock<IEmailSender> _emailsender;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsReferralServiceMock;
        private readonly Mock<IODSLookupService> _wmsODSLookupServiceMock;
        private readonly WmsCalculations _selfReferralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        private readonly Mock<IConfiguration> _configuration;

        public AuthControllerTests()
        {
            _loggerMock = new Mock<ILogger<AuthController>>();
            _emailsender = new Mock<IEmailSender>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _wmsReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _selfReferralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsODSLookupServiceMock = new Mock<IODSLookupService>();
            _configuration = new Mock<IConfiguration>();

            _classUnderTest = new AuthController(
                _loggerMock.Object,
                _emailsender.Object,
                _wmsReferralServiceMock.Object,  
                _wmsODSLookupServiceMock.Object,
                mockTelemetryClient,
                _configuration.Object
                );
        }

        [Fact]
        public void Index_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() {  };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.Index();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((AuthViewModel)outputResult.ViewData.Model).EmailAddress);
        }
        [Fact]
        public async Task Index_PostReturnsRedirectEmailNotValid()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@somewhere.com" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = await _classUnderTest.Index(authmodel);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("InvalidEmail", outputResult.ViewName);
            
        }

        [Fact]
        public async Task RequestNewToken_ReturnsRedirectValidateCode() 
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };
           
            //setup configuration
            Mock<IConfigurationSection> mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("30");            
            _configuration.Setup(x => x.GetSection(It.Is<string>(k => k == "PharmacyReferral:TokenExpiry"))).Returns(mockSection.Object);

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //mock api
            _wmsReferralServiceMock.Setup(x => x.GenerateOTPTokenAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(@"{ 'keycode': '', 'expires': "+ JsonConvert.SerializeObject(DateTime.UtcNow.AddMinutes(30)) +" }"))));
            

            //Act
            var result = await _classUnderTest.RequestNewToken("something@nhs.net");

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ValidateCode", outputResult.ViewName);
        }

        [Fact]
        public void ValidateCode_ReturnsView() 
        { 
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.ValidateCode();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(outputResult.ViewName);            
        }
        [Fact]
        public void ValidateCode_ReturnsAuthorisedView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised=true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.ValidateCode();

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("lookup-pharmacy", outputResult.ActionName);
        }

        [Fact]
        public async Task ValidateCodePost_ReturnValid()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //mock api
            _wmsReferralServiceMock.Setup(x => x.ValidateOTPTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(@"{ 'validCode': true, 'keyCode': '', 'expires': " + JsonConvert.SerializeObject(DateTime.UtcNow.AddMinutes(30)) + " }"))));


            //Act
            var result = await _classUnderTest.ValidateCode(authmodel);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("lookup-pharmacy", outputResult.ActionName);
        }

        [Fact]
        public async Task ValidateCodePost_ReturnInValid()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //mock api
            _wmsReferralServiceMock.Setup(x => x.ValidateOTPTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.UnprocessableEntity, new StringContent(TestSetup.ApiError()))));


            //Act
            var result = await _classUnderTest.ValidateCode(authmodel);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);  //return invalid modelstate          
            Assert.Null(outputResult.ViewName); //null view, i.e. current view
        }

        [Fact]
        public void LookupPharmacy_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised=true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.LookupPharmacy();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(outputResult.ViewName); //null view, i.e. current view
        }
        [Fact]
        public void LookupPharmacy_ReturnsUnauthorizedView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.LookupPharmacy();

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("begin", outputResult.ActionName); //redirect to begin
        }

        [Fact]
        public async Task LookupPharmacyPost_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised = true, ODSCode="F1111" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //mock api
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode=200, Status="Active" }));


            //Act
            var result = await _classUnderTest.LookupPharmacy(authmodel);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("confirm-pharmacy", outputResult.ActionName); //redirect to confirm
        }

        [Theory]
        [InlineData("F1111")]
        [InlineData("A1111")]        
        public async Task LookupPharmacyPost_ReturnsInvalidCode(string ODSCode)
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised = true, ODSCode = ODSCode };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //mock api
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 404 }));


            //Act
            var result = await _classUnderTest.LookupPharmacy(authmodel);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);  //return invalid modelstate          
            Assert.Null(outputResult.ViewName); //null view, i.e. current view
        }

        [Fact]
        public void ConfirmPharmacy_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@somewhere.com", IsAuthorised=true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.ConfirmPharmacy();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);                    
            Assert.Null(outputResult.ViewName); //null view, i.e. current view
        }

        //[Fact]
        //public async Task RequestNewToken_ReturnsRedirectEmailNotValid()
        //{
        //    //Arrange                        
        //    AuthViewModel authmodel = new() { EmailAddress = "something@somewhere.com" };

        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(authmodel).Object;

        //    //Act
        //    var result = await _classUnderTest.Index(authmodel);

        //    //Assert
        //    var outputResult = Assert.IsType<ViewResult>(result);
        //    Assert.Equal("InvalidEmail", outputResult.ViewName);
        //}


        
    }

    
}
