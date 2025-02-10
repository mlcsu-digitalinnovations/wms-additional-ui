using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsStaffReferral.Controllers;
using WmsStaffReferral.Models;
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

namespace WmsReferral.Tests.StaffReferral.Controllers
{
    public class StaffReferralControllerTests
    {
        private readonly StaffReferralController _classUnderTest;
        private readonly Mock<ILogger<StaffReferralController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsSelfReferralServiceMock;
        private readonly Mock<IPostcodesioService> _wmsPostCodeioServiceMock;
        private readonly WmsCalculations _selfReferralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        public StaffReferralControllerTests()
        {
            _loggerMock = new Mock<ILogger<StaffReferralController>>();
            TelemetryClient mockTelemetryClient = InitializeMockTelemetryChannel();
            _wmsSelfReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _selfReferralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsPostCodeioServiceMock = new Mock<IPostcodesioService>();

            _classUnderTest = new StaffReferralController(
                _loggerMock.Object,
                _wmsSelfReferralServiceMock.Object,
                _wmsPostCodeioServiceMock.Object,
                mockTelemetryClient,
                _selfReferralBusiness
                );
        }

        [Fact]
        public async Task EmailAddressPost_ReturnsRedirectToAction_EmailNotEligible()
        {
            //Arrange            
            StaffEmailAddressViewModel model = new() { Email = "something@somewhere.com" };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@somewhere.com" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("not-eligible-for-service", outputResult.ActionName);
        }

        [Fact]
        public async Task EmailAddressPost_ReturnsRedirectToAction_EmailConflict()
        {
            //Arrange            
            StaffEmailAddressViewModel model = new() { Email = "something@nhsorg.nhs.uk" };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.EmailInUseAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.Conflict, new StringContent(ApiError()))));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error: Thank you. Your referral has already been submitted. If you " +
              "completed the self-referral form but did not continue to select a provider, you " +
              "will receive a link via text message to allow you to do this. Please wait up to " +
              "two working days.",
              ((ErrorViewModel)outputResult.Model).Message);
        }
        [Fact]
        public async Task EmailAddressPost_ReturnsRedirectToAction_EmailOk()
        {
            //Arrange            
            StaffEmailAddressViewModel model = new() { Email = "something@nhsorg.nhs.uk" };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //mock api            
            _wmsSelfReferralServiceMock.Setup(x => x.EmailInUseAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(ApiError()))));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("height", outputResult.ActionName);
        }
        [Fact]
        public void HeightImperialPost_ReturnsModelError_HeightNotValid()
        {
            //Arrange            
            HeightImperialViewModel model = new() { HeightFt = 0, HeightIn = 0 };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.HeightImperial(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("HeightError"));
        }
        [Fact]
        public void HeightImperialGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk", HeightCm = 100 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.HeightImperial();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((HeightImperialViewModel)outputResult.ViewData.Model).HeightFt);
        }
        [Fact]
        public void WeightImperialGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk", WeightKg = 100 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.WeightImperial();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((WeightImperialViewModel)outputResult.ViewData.Model).WeightSt);
        }
        [Fact]
        public void HeightGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk", HeightCm = 100 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.Height();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((HeightViewModel)outputResult.ViewData.Model).Height);
        }
        [Fact]
        public void WeightGet_ReturnsView_Valid()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk", WeightKg = 100 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.Weight();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((WeightViewModel)outputResult.ViewData.Model).Weight);
        }
        [Fact]
        public void WeightImperialPost_ReturnsModelError_WeightNotValid()
        {
            //Arrange            
            WeightImperialViewModel model = new() { WeightLb = 0, WeightSt = 0 };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.WeightImperial(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        [Fact]
        public async Task EthnicityPost_ReturnsRedirectToActionToFamilyName()
        {
            //Arrange            
            EthnicityViewModel model = new()
            {
                ReferralEthnicity = "White",
                ReferralEthnicityGroup = "White"
            };

            WmsReferral.Business.Models.StaffReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 120,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White"
            };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.Ethnicity(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("family-name", outputResult.ActionName);
        }
        [Fact]
        public async Task StaffRolesPost_ReturnsRedirectToAction()
        {
            //Arrange            
            StaffRoleViewModel model = new() { StaffRole = "Nurse" };
            WmsReferral.Business.Models.StaffReferral referral = new()
            {
                Email = "test@test.com",
                StaffRole = "Nurse"
            };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.GetStaffRolesAsync())
                .Returns(Task.FromResult((IEnumerable<StaffRole>)GetStaffRoles()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.StaffRole(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("consent-for-future-contact", outputResult.ActionName);
        }
        [Fact]
        public async Task StaffRolesGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new()
            {
                Email = "test@test.com",
                StaffRole = "Nurse"
            };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.GetStaffRolesAsync())
                .Returns(Task.FromResult((IEnumerable<StaffRole>)GetStaffRoles()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.StaffRole();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((StaffRoleViewModel)outputResult.Model).StaffRole);
            Assert.NotNull(((StaffRoleViewModel)outputResult.Model).StaffRoleList);
        }
        [Fact]
        public void WeightDateTakenPost_ReturnsModelError_DateNotValid()
        {
            //Arrange            
            WeightDateTakenViewModel model = new() { Day = 0, Month = 0, Year = 0 };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.WeightDateTaken(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        }
        [Fact]
        public void DateOfBirthPost_ReturnsModelError_DateNotValid()
        {
            //Arrange            
            DateOfBirthViewModel model = new() { Day = 0, Month = 0, Year = 0 };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.DateofBirth(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        }
        [Fact]
        public async Task AddressPost_ReturnsModelError_PostCodeNotFound()
        {
            //Arrange            
            AddressViewModelV1 model = new() { Postcode = "Z1 AB" };
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "test@test.com" };

            //mock api
            _wmsPostCodeioServiceMock.Setup(x => x.ValidPostCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = await _classUnderTest.Address(model);

            //Assert 
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewData.ModelState.ContainsKey("Postcode"));
        }
        [Fact]
        public void AddressGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.StaffReferral referral = new() { Email = "test@test.com", Address1 = "Address 1", Postcode = "Z1 1AA" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.Address();

            //Assert 
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Postcode);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Address1);
        }
        [Fact]
        public void CheckAnswersGetRefSubmitted_ReturnsAction()
        {
            //Arrange
            WmsReferral.Business.Models.StaffReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 100,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White"
            };

            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,
                AnsweredPhysicalDisability = true,
                ReferralSubmitted = true
            };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.CheckAnswers();

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("provider-choice", outputResult.ActionName);
        }
        [Fact]
        public void CheckAnswersGet_ReturnsView()
        {
            //Arrange
            WmsReferral.Business.Models.StaffReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 100,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White"
            };

            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,
                AnsweredPhysicalDisability = true
            };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel()).Object;

            //Act
            var result = _classUnderTest.CheckAnswers();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((CheckAnswersViewModel)outputResult.ViewData.Model).KeyAnswer);
        }
        [Fact]
        public async Task CheckAnswersPost_ReturnsOkAction()
        {
            //Arrange
            WmsReferral.Business.Models.StaffReferral referral = new()
            {                
                Email = "test@test.com",
                HeightCm = 120,
                WeightKg = 100                
            };
            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,
                AnsweredPhysicalDisability = true
            };
            CheckAnswersViewModel model = new()
            {
                Referral = referral,
                KeyAnswer = keyAnswer
            };
            ProviderChoiceModel providerChoices = new() { Id = new Guid(), ProviderId = new Guid(), ProviderChoices = new List<Provider>() };


            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(referral, keyAnswer, providerChoices).Object;

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.AddStaffReferralAsync(It.IsAny<WmsReferral.Business.Models.StaffReferral>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(providerChoices)))));

            //Act
            var result = await _classUnderTest.CheckAnswers(model);

            //Assert            
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("provider-choice", outputResult.ActionName);
        }

        [Fact]
        public void ProviderChoiceGet_ReturnsView()
        {
            //Arrange            
            ProviderChoiceModel providerChoices = new() { Id = Guid.NewGuid(), ProviderId = Guid.NewGuid(), ProviderChoices = new List<Provider>() };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(new WmsReferral.Business.Models.StaffReferral(), new KeyAnswer(), providerChoices).Object;
            
            //Act
            var result = _classUnderTest.ProviderChoice();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((ProviderChoiceModel)outputResult.ViewData.Model).ProviderChoices);
        }

        [Fact]
        public void ProviderChoicePost_ReturnsView()
        {
            //Arrange
            var provGuid = Guid.NewGuid();
            ProviderChoiceModel providerChoices = new() { Id = Guid.NewGuid(), ProviderId = provGuid, ProviderChoices = new List<Provider>() { new Provider() { Id= provGuid } } };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(new WmsReferral.Business.Models.StaffReferral(), new KeyAnswer(), providerChoices).Object;

            //Act
            var result = _classUnderTest.ProviderChoice(providerChoices);

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((ProviderChoiceModel)outputResult.ViewData.Model).ProviderChoices);
        }

        [Fact]
        public async Task ProviderConfirmPost_ReturnsOkResult()
        {
            //Arrange
            var provGuid = Guid.NewGuid();
            ProviderChoiceModel providerChoices = new() { 
                Id = Guid.NewGuid(), 
                ProviderId = provGuid, 
                ProviderChoices = new List<Provider>() { 
                    new Provider() 
                    { 
                        Id = provGuid 
                    }
                }, 
                Provider = new Provider() { Id = provGuid } };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = InitializeControllerContext(new WmsReferral.Business.Models.StaffReferral(), new KeyAnswer(), providerChoices).Object;
                        
            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.UpdateProviderChoiceAsync(It.IsAny<ProviderChoiceModel>(), It.IsAny<string>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(providerChoices)))));

            //Act
            var result = await _classUnderTest.ProviderConfirm(providerChoices);

            //Assert            
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("complete", outputResult.ActionName);
        }

        private static Mock<HttpContext> InitializeControllerContext(WmsReferral.Business.Models.StaffReferral referral, KeyAnswer keyAnswer, ProviderChoiceModel providerChoiceModel)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            var sessionReferral = JsonConvert.SerializeObject(referral);
            byte[] srbytes = System.Text.Encoding.UTF8.GetBytes(sessionReferral);

            var sessionKeyAnswer = JsonConvert.SerializeObject(keyAnswer);
            byte[] skabytes = System.Text.Encoding.UTF8.GetBytes(sessionKeyAnswer);

            var sessionProviderChoice = JsonConvert.SerializeObject(providerChoiceModel);
            byte[] spcbytes = System.Text.Encoding.UTF8.GetBytes(sessionProviderChoice);

            mockSession.Setup(x => x.TryGetValue("StaffReferral", out srbytes)).Returns(true);
            mockSession.Setup(x => x.TryGetValue("Answers", out skabytes)).Returns(true);
            mockSession.Setup(x => x.TryGetValue("ProviderChoice", out spcbytes)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);

            return mockContext;
        }

        private static List<Ethnicity> GetEthnicities()
        {
            List<Ethnicity> ethnicities = new()
            {
                new Ethnicity { DisplayName = "White", GroupName = "White", TriageName = "White", DisplayOrder = 1, GroupOrder = 1 }
            };
            return ethnicities;
        }
        private static List<StaffRole> GetStaffRoles()
        {
            List<StaffRole> staffRoles = new()
            {
                new StaffRole { DisplayName = "Nurse", DisplayOrder = 1 }
            };
            return staffRoles;
        }

        private static HttpResponseMessage HttpResponseMessageMock(HttpStatusCode code, HttpContent content)
        {
            return new HttpResponseMessage() { StatusCode = code, Content = content };
        }
        private static string ApiError()
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

        private static string ProviderChoice()
        {
            return @"{
              'Id': '',
              'ProviderId': '',
              'Ubrn' : '',
              'Token': '',             
              'ProviderChoices': {
                        'Id': 'val1',
                        'Name': ' ',
                        'Summary': ' ',
                        'Summary2': ' ',
                        'Summary3': ' ',
                        'Website': ' ',
                        'Logo': ' ',
                        '': ' ',
                        '': ' ',
                        '': ' ',
                        }
            }";
        }

        private static TelemetryClient InitializeMockTelemetryChannel()
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
