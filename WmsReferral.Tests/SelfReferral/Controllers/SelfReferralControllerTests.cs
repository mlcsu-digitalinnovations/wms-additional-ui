using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsSelfReferral.Controllers;
using WmsSelfReferral.Models;
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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using IdentityModel.Client;
using System.Security.Claims;
using System.Net.Mime;
using System.Threading;
using Microsoft.Extensions.Configuration;
using WmsSelfReferral.Data;
using WmsSelfReferral.Helpers;

namespace WmsReferral.Tests.SelfReferralTests.Controllers
{
    public class SelfReferralControllerTests
    {
        private readonly SelfReferralController _classUnderTest;
        private readonly Mock<ILogger<SelfReferralController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsSelfReferralServiceMock;
        private readonly Mock<IPostcodesioService> _wmsPostCodeioServiceMock;
        private readonly WmsCalculations _selfReferralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        private readonly Mock<IGetAddressioService> _GetAddressioService;
        private readonly INhsLoginService _nhsLoginService;
        private readonly Mock<ITempDataDictionary> _tempData;
        private readonly Mock<IUrlHelperFactory> _routing;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IReferralSessionData> _referralSessionData;
        public SelfReferralControllerTests()
        {
            _loggerMock = new Mock<ILogger<SelfReferralController>>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _wmsSelfReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _selfReferralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsPostCodeioServiceMock = new Mock<IPostcodesioService>();
            _GetAddressioService = new Mock<IGetAddressioService>();

            _tempData = new Mock<ITempDataDictionary>();
            _routing = new Mock<IUrlHelperFactory>();

            _nhsLoginService = new NhsLoginService(MockNhsLoginClient(), mockTelemetryClient, TestSetup.InitializeConfiguration());
            //_nhsLoginService = new Mock<INhsLoginService>();
            _configuration = new Mock<IConfiguration>();
            _referralSessionData = new Mock<IReferralSessionData>();


            _classUnderTest = new SelfReferralController(
                _loggerMock.Object,
                _wmsSelfReferralServiceMock.Object,
                _wmsPostCodeioServiceMock.Object,
                mockTelemetryClient,
                _selfReferralBusiness,
                _nhsLoginService,
                _GetAddressioService.Object,
                _configuration.Object,
                _referralSessionData.Object
                )
            {
                TempData = _tempData.Object
            };

        }

        //[Fact]
        //public async Task ConsentNHSNUmberGP_ReturnsInvalidView()
        //{
        //    //Arrange                        
        //    SelfReferral referral = new() { NhsNumber = "4010232137" };

        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;
        //    //mock api
        //    _wmsSelfReferralServiceMock.Setup(x => x.NhsNumberInUseAsync(It.IsAny<string>()))
        //        .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.Conflict, new StringContent(ApiError()))));

        //    //Act
        //    var result = await _classUnderTest.ConsentNhsNumberGP();

        //    //Assert
        //    var outputResult = Assert.IsType<ViewResult>(result);

        //    Assert.Equal("NotEligibleReferralExists", outputResult.ViewName);
        //}
        //[Fact]
        //public void ConsentNHSNUmberGPPost_ReturnsInvalidView()
        //{
        //    //Arrange                        
        //    SelfReferral referral = new() { };
        //    ConsentNHSNumberGPPracticeViewModel model = new() { ConsentToLookups = "false" };
        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

        //    //Act
        //    var result = _classUnderTest.ConsentNhsNumberGP(model);

        //    //Assert
        //    var outputResult = Assert.IsType<RedirectToActionResult>(result);
        //    Assert.Equal("Not-Eligible-For-Service", outputResult.ActionName);
        //}

        [Fact]
        public async Task Index_ReturnsValidView()
        {
            //Arrange
            SelfReferral referral = new()
            {
                NhsNumber="1234"
            };

            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,
                AnsweredPhysicalDisability = true,
                AnsweredConsentForFurtureContact = true,
                AnsweredPatientVulnerable = true
            };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //setup tempdata
            TempDataDictionary tempData = new(_classUnderTest.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            tempData[ControllerConstants.TEMPDATA_LINK_ID] = "testid999999";
            _classUnderTest.TempData = tempData;

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.NhsNumberCheckAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new NhsNumberCheck() { Referral = referral, StatusCode=200 }));
            _wmsSelfReferralServiceMock.Setup(x => x.ValidateLinkId(It.IsAny<string>()))
                .ReturnsAsync(true);

            //Act
            var result = await _classUnderTest.Index();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewName == "updatereferral");
            Assert.NotNull(((NhsLoginViewModel)outputResult.ViewData.Model).Nhs_number);
        }

        [Fact]
        public async Task Index_LinkIdIsInvalid_ReturnsGoneWrong()
        {
            //Arrange
            Mock<HttpContext> mockContext = new();
            _classUnderTest.ControllerContext.HttpContext = mockContext.Object;
            TempDataDictionary tempData = new(_classUnderTest.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>())
            {
                [ControllerConstants.TEMPDATA_LINK_ID] = "testid012345"
            };
            _classUnderTest.TempData = tempData;

            //Act
            IActionResult result = await _classUnderTest.Index();

            //Assert
            ViewResult outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewName == "GoneWrong");
            Assert.True(((ErrorViewModel)outputResult.ViewData.Model).Message == "Error: User link ID is not a valid link ID.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Index_LinkIdIsNullOrWhiteSpace_ReturnsGoneWrong(string linkId)
        {
            //Arrange
            Mock<HttpContext> mockContext = new();
            _classUnderTest.ControllerContext.HttpContext = mockContext.Object;
            TempDataDictionary tempData = new(_classUnderTest.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>())
            {
                [ControllerConstants.TEMPDATA_LINK_ID] = linkId
            };
            _classUnderTest.TempData = tempData;

            //Act
            IActionResult result = await _classUnderTest.Index();

            //Assert
            ViewResult outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewName == "GoneWrong");
            Assert.True(((ErrorViewModel)outputResult.ViewData.Model).Message == "Error: User link ID is missing from URL.");
        }

        [Fact]
        public async Task Index_ValidateLinkIdReturnsFalse_ReturnsGoneWrong()
        {
            //Arrange
            Mock<HttpContext> mockContext = new();
            _classUnderTest.ControllerContext.HttpContext = mockContext.Object;
            TempDataDictionary tempData = new(_classUnderTest.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>())
            {
                [ControllerConstants.TEMPDATA_LINK_ID] = "testid999999"
            };
            _classUnderTest.TempData = tempData;
            _wmsSelfReferralServiceMock.Setup(x => x.ValidateLinkId(It.IsAny<string>()))
                .ReturnsAsync(false);

            //Act
            IActionResult result = await _classUnderTest.Index();

            //Assert
            ViewResult outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewName == "GoneWrong");
            Assert.True(((ErrorViewModel)outputResult.ViewData.Model).Message == "Error: Unable to match user link ID to referral.");
        }

        [Theory]
        [InlineData("false")]
        [InlineData("true")]
        public void ConsentReferrerUpdatePost_ReturnsView(string consent)
        {
            //Arrange                        
            SelfReferral referral = new() { };
            ConsentForReferrerUpdateViewModel model = new() { ConsentToReferrerUpdate = consent };
            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //Act
            var result = _classUnderTest.ConsentReferrerUpdate(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("medical-conditions", outputResult.ActionName);
        }

        [Fact]
        public async Task HeightImperialPost_ReturnsModelError_HeightNotValid()
        {
            //Arrange            
            HeightImperialViewModel model = new() { HeightFt = 0, HeightIn = 0 };
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.HeightImperial(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("HeightError"));
        }
        [Fact]
        public void HeightImperialGet_ReturnsValidView()
        {
            //Arrange                        
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, HeightCm = 100 };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

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
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, WeightKg = 100 };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

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
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, HeightCm = 100, DateOfBirth = new DateTime(2000,1,1) };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.NhsNumberInUseAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(""))));

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //Act
            var result = _classUnderTest.Height();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((HeightViewModel)outputResult.ViewData.Model).Height);
        }
        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(400)]
        public async Task HeightPost_ReturnsInValidView(int? heightCm)
        {
            //Arrange
            HeightViewModel model = new() { Height = heightCm };
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, HeightCm = heightCm };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Height(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("Height"));
        }
        [Fact]
        public void WeightGet_ReturnsView_Valid()
        {
            //Arrange                        
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, WeightKg = 100 };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //Act
            var result = _classUnderTest.Weight();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((WeightViewModel)outputResult.ViewData.Model).Weight);
        }
        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(34)]
        [InlineData(-1)]
        [InlineData(501)]
        public async Task WeightPost_ReturnsInValidView(int? weightKg)
        {
            //Arrange
            WeightViewModel model = new() { Weight = weightKg };
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, WeightKg = weightKg };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Weight(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        [Fact]
        public async Task WeightImperialPost_ReturnsModelError_WeightNotValid()
        {
            //Arrange            
            WeightImperialViewModel model = new() { WeightLb = 0, WeightSt = 0 };
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.WeightImperial(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        [Fact]
        public async Task EthnicityGroupGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new()
            {
                ConsentForGpAndNhsNumberLookup = true
            };

            //mock api, api returns 0 items
            _wmsSelfReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));

            //Act
            var result = await _classUnderTest.EthnicityGroup();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task EthnicityGroupPost_ReturnsDobView()
        {
            //Arrange                    
            SelfReferral referral = new()
            {
                ConsentForGpAndNhsNumberLookup = true
            };

            EthnicityViewModel model = new() { ReferralEthnicityGroup = "I do not wish to Disclose my Ethnicity" };

            //mock api, api returns 0 items
            _wmsSelfReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //Act
            var result = await _classUnderTest.EthnicityGroup(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("date-of-birth", outputResult.ActionName);
        }
        [Fact]
        public async Task Ethnicity_IdNullReturnsGoneWrong()
        {
            //Arrange 
            SelfReferral referral = new()
            {
                ConsentForGpAndNhsNumberLookup = true
            };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Ethnicity(id: null);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
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

            SelfReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 120,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White",
                ConsentForGpAndNhsNumberLookup = true
            };

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //Act
            var result = await _classUnderTest.Ethnicity(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("date-of-birth", outputResult.ActionName);
        }
        //[Fact]
        //public void WeightDateTakenGet_ReturnsView()
        //{
        //    //Arrange
        //    SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };

        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

        //    //Act
        //    var result = _classUnderTest.WeightDateTaken();

        //    //Assert
        //    var outputResult = Assert.IsType<ViewResult>(result);

        //    Assert.Null(outputResult.Model);
        //}
        //[Fact]
        //public async Task WeightDateTakenPost_ReturnsModelError_DateNotValid()
        //{
        //    //Arrange            
        //    WeightDateTakenViewModel model = new() { Day = 0, Month = 0, Year = 0 };
        //    SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };

        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

        //    //Act
        //    var result = await _classUnderTest.WeightDateTaken(model);

        //    //Assert
        //    var outputResult = Assert.IsType<ViewResult>(result);

        //    Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        //}
        [Fact]
        public async Task DateOfBirthGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));

            //Act
            var result = await _classUnderTest.DateofBirth();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task DateOfBirthPost_ReturnsModelError_DateNotValid()
        {
            //Arrange            
            DateOfBirthViewModel model = new() { Day = 0, Month = 0, Year = 0 };
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.DateofBirth(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        }
        [Fact]
        public async Task SexAtBirthGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));

            //Act
            var result = await _classUnderTest.Sex();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }

        [Theory]
        [InlineData("Male")]
        [InlineData("Female")]
        public async Task SexAtBirthPost_ReturnsSuccessAction(string sex)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            SexViewModel model = new() { Sex = sex };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Sex(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("bariatric-surgery", outputResult.ActionName);
        }
        [Fact]
        public async Task BariatricSurgeryGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            
            //Act
            var result = await _classUnderTest.BariatricSurgery();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("Yes")]
        [InlineData("No")]
        public async Task BariatricSurgeryPost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            BariatricSurgeryViewModel model = new() { BariatricSurgery = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //setup session/context
            //_classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.BariatricSurgery(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("active-eating-disorder", outputResult.ActionName);
        }
        [Fact]
        public async Task EatingDisorderGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.EatingDisorder();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("Yes")]
        [InlineData("No")]
        public async Task EatingDisorderPost_ReturnsSuccessActionFemale(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, Sex = "Female" };
            EatingDisorderViewModel model = new() { EatingDisorder = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(false);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.EatingDisorder(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("are-you-pregnant", outputResult.ActionName);
        }
        [Theory]
        [InlineData("Yes")]
        [InlineData("No")]
        public async Task EatingDisorderPost_ReturnsSuccessActionMale(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true, Sex = "Male" };
            EatingDisorderViewModel model = new() { EatingDisorder = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(false);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.EatingDisorder(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("family-name", outputResult.ActionName);
        }

        [Fact]
        public async Task PregnantGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Pregnant();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("Yes")]
        [InlineData("No")]
        public async Task PregnantPost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            PregnantViewModel model = new() { Pregnant = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Pregnant(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("family-name", outputResult.ActionName);
        }
        

        [Fact]
        public async Task FamilyNameGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.FamilyName();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("Familyname")]
        public async Task FamilyNamePost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            FamilyNameViewModel model = new() { FamilyName = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(false);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.FamilyName(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("given-name", outputResult.ActionName);
        }
        [Fact]
        public async Task GivenNameGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.GivenName();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("Givenname")]
        public async Task GivenNamePost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            GivenNameViewModel model = new() { GivenName = answer };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(false);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.GivenName(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("find-address", outputResult.ActionName);
        }
        [Fact]
        public async Task FindAddressGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.FindAddress();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("404")]
        [InlineData("400")]
        public async Task FindAddressPost_ReturnsModelError(string errorcodes)
        {
            //Arrange                    
            SelfReferral referral = new() { };
            FindAddressViewModel model = new() { Postcode = "" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;
            //mock GetAddressIO
            _GetAddressioService.Setup(x => x.GetAddressList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Error", errorcodes)
                }));

            //Act
            var result = await _classUnderTest.FindAddress(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewData.ModelState.ContainsKey("Postcode"));
        }


        [Fact]
        public async Task AddressPost_ReturnsModelError_PostCodeNotFound()
        {
            //Arrange            
            AddressViewModelV1 model = new() { Postcode = "Z1 AB" };
            SelfReferral referral = new() { Email = "test@test.com", ConsentForGpAndNhsNumberLookup = true, Postcode = "" };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //mock api
            _wmsPostCodeioServiceMock.Setup(x => x.ValidPostCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

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
            SelfReferral referral = new() { Email = "test@test.com", Address1 = "Address 1", Postcode = "Z1 1AA" };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = _classUnderTest.Address();

            //Assert 
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Postcode);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Address1);
        }
        [Fact]
        public async Task EmailAddressGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.EmailAddress();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Theory]
        [InlineData("some")]
        [InlineData("some.some")]
        [InlineData("s.i.f.t")]
        [InlineData("@")]
        [InlineData("some.@some.l")]
        public void EmailAddress_ReturnsModelError(string email)
        {
            //Arrange                        
            SelfReferral referral = new() { };
            EmailAddressViewModel model = new() { Email = email };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task MobileGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Mobile();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task MobilePost_ReturnsModelError()
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            MobileViewModel model = new() { Mobile = "+44" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;
            _classUnderTest.ModelState.AddModelError("Mobile", "");
            //Act
            var result = await _classUnderTest.Mobile(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }
        [Fact]
        public async Task TelephoneGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.Telephone();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task TelephonePost_ReturnsModelError()
        {
            //Arrange                    
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            TelephoneViewModel model = new() { Telephone = "" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;
            _classUnderTest.ModelState.AddModelError("Telephone", "");
            //Act
            var result = await _classUnderTest.Telephone(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }
        [Fact]
        public async Task MedicalConditionsGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.MedicalConditions();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task MedicalConditionsPost_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };
            MedicalConditionsViewModel model = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.MedicalConditions(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
       
        [Fact]
        public async Task PhysicalDisabilityGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.PhysicalDisability();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task LearningDisabilityGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.LearningDisability();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task ConsentFurtureContactGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            SelfReferral referral = new() { };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.ConsentFutureContact();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }

        [Fact]
        public async Task CheckAnswersGetRefSubmitted_ReturnsAction()
        {
            //Arrange
            SelfReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 100,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White",
                ConsentForGpAndNhsNumberLookup = true
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
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(keyAnswer);
            _referralSessionData.Setup(x => x.GetProviderChoiceSessionData()).Returns(new ProviderChoiceModel());
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(true);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.CheckAnswers();

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("provider-choice", outputResult.ActionName);
        }
        [Fact]
        public async Task CheckAnswersGet_ReturnsView()
        {
            //Arrange
            SelfReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 100,
                WeightKg = 100,
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White",
                ConsentForGpAndNhsNumberLookup = true
            };

            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,
                AnsweredPhysicalDisability = true,
                AnsweredConsentForFurtureContact = true,
                AnsweredPregnant = true,
                AnsweredBariatricSurgery= true,
                AnsweredEatingDisorder= true
            };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(keyAnswer);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(true);
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel(), "SelfReferral").Object;

            //Act
            var result = await _classUnderTest.CheckAnswers();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((CheckAnswersViewModel)outputResult.ViewData.Model).KeyAnswer);
        }
        [Fact]
        public async Task CheckAnswersPost_ReturnsOkAction()
        {
            //Arrange
            SelfReferral referral = new()
            {
                Email = "test@test.com",
                ConsentForGpAndNhsNumberLookup = true,
                HeightCm = 120,
                WeightKg = 100,
                IsPregnant=false,
                HasActiveEatingDisorder=false,
                HasHadBariatricSurgery=false
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
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(keyAnswer);
            _referralSessionData.Setup(x=>x.GetProviderChoiceSessionData()).Returns(providerChoices);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(true));
            _referralSessionData.Setup(x => x.ReferralCompleted()).Returns(true);

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.AddSelfReferralAsync(It.IsAny<SelfReferral>()))
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
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(new SelfReferral());
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.GetProviderChoiceSessionData()).Returns(providerChoices);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

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
            ProviderChoiceModel providerChoices = new() { Id = Guid.NewGuid(), ProviderId = provGuid, ProviderChoices = new List<Provider>() { new Provider() { Id = provGuid } } };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(new SelfReferral());
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.GetProviderChoiceSessionData()).Returns(providerChoices);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

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
            ProviderChoiceModel providerChoices = new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provGuid,
                ProviderChoices = new List<Provider>() {
                    new Provider()
                    {
                        Id = provGuid
                    }
                },
                Provider = new Provider() { Id = provGuid }
            };

            //setup session/context
            _referralSessionData.Setup(x => x.GetReferralSessionData()).Returns(new SelfReferral());
            _referralSessionData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _referralSessionData.Setup(x => x.GetProviderChoiceSessionData()).Returns(providerChoices);
            _referralSessionData.Setup(x => x.NotValidSession()).Returns(Task.FromResult(false));

            //mock api
            _wmsSelfReferralServiceMock.Setup(x => x.UpdateProviderChoiceAsync(It.IsAny<ProviderChoiceModel>(), It.IsAny<string>()))
                .Returns(Task.FromResult(HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(providerChoices)))));

            //Act
            var result = await _classUnderTest.ProviderConfirm(providerChoices);

            //Assert            
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("complete", outputResult.ActionName);
        }

        private static List<KeyValuePair<string, string>> GetEthnicityGroups()
        {
            List<KeyValuePair<string, string>> ethgroups = new() { new KeyValuePair<string, string>("", "") };
            return ethgroups;
        }
        private static List<Ethnicity> GetEthnicities()
        {
            List<Ethnicity> ethnicities = new()
            {
                new Ethnicity { DisplayName = "White", GroupName = "White", TriageName = "White", DisplayOrder = 1, GroupOrder = 1 }
            };
            return ethnicities;
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

        private static HttpClient MockNhsLoginClient()
        {

            var nhsLogin = new NhsLoginViewModel()
            {
                Email = "something@something.com",
                Email_verified = true,
                Family_name = "none",
                Given_name = "none",
                Identity_proofing_level = "P5",
                Phone = "021212",
                Phone_number_verified = true,
                Nhs_number = "0",
                Birthdate = new DateTime(1980, 1, 1),
                Gp_registration_details = new() { Gp_ods_code = "C12345" }
            };
            var messageHandler = new MockHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(nhsLogin), Encoding.UTF8, MediaTypeNames.Application.Json)
            });

            var httpClient = new HttpClient(messageHandler);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return httpClientFactoryMock.Object.CreateClient("nhslogin");
        }

        private static IConfiguration MockConfiguration()
        {
            Mock<IConfigurationSection> mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("ConfigValue");

            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(x => x.GetSection(It.Is<string>(k => k == "ConfigKey"))).Returns(mockSection.Object);

            return mockConfig.Object;
        }


    }

    public class MockHttpMessageHandler : DelegatingHandler
    {
        private HttpResponseMessage _fakeResponse;

        public MockHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            _fakeResponse = responseMessage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(_fakeResponse);
        }
    }
}
