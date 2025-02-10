using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WmsMskReferral.Controllers;
using WmsMskReferral.Models;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using Xunit;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace WmsReferral.Tests.MskReferralTests.Controllers
{
    public class MskReferralControllerTests
    {
        private readonly MskReferralController _classUnderTest;
        private readonly Mock<IEmailSender> _emailsender;
        private readonly Mock<ILogger<MskReferralController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsReferralServiceMock;
        private readonly Mock<IPostcodesioService> _wmsPostCodeioServiceMock;
        private readonly Mock<IODSLookupService> _wmsODSLookupServiceMock;
        private readonly WmsCalculations _referralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        private readonly Mock<IGetAddressioService> _GetAddressioService;

        private readonly Mock<ITempDataDictionary> _tempData;
        private readonly Mock<IUrlHelperFactory> _routing;

        public MskReferralControllerTests()
        {
            _loggerMock = new Mock<ILogger<MskReferralController>>();
            _emailsender = new Mock<IEmailSender>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _wmsReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _wmsODSLookupServiceMock = new Mock<IODSLookupService>();
            _referralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsPostCodeioServiceMock = new Mock<IPostcodesioService>();
            _GetAddressioService = new Mock<IGetAddressioService>();


            _tempData = new Mock<ITempDataDictionary>();
            _routing = new Mock<IUrlHelperFactory>();

            _classUnderTest = new MskReferralController(
                _loggerMock.Object,
                _emailsender.Object,
                _wmsReferralServiceMock.Object,
                _wmsPostCodeioServiceMock.Object,
                _wmsODSLookupServiceMock.Object,
                mockTelemetryClient,
                _referralBusiness,
                _GetAddressioService.Object
                )
            {
                TempData = _tempData.Object
            };


            


        }

        [Fact]
        public async Task StartOver_ReturnsValidView()
        {
            //Arrange
            MskReferral referral = new()
            {
                NhsNumber = "1234"
            };

            MskHubViewModel mskHubauth = new()
            {
                IsAuthorised = true
            };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;


            //Act
            var result = await _classUnderTest.StartOver();

            //Assert            
            var outputResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.True(outputResult.ActionName == "Index");
        }
        [Fact]
        public async Task Index_ReturnsValidView()
        {
            //Arrange
            MskReferral referral = new()
            {
                NhsNumber = "1234"
            };

            MskHubViewModel mskHubauth = new()
            {
                IsAuthorised = true,
                SelectedMskHub = "hubselected",
                NameIdentifier = "490t590j"

            };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral",mskHubViewModel: mskHubauth).Object;
                       

            //Act
            var result = await _classUnderTest.Index();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            
            Assert.True(((MskHubViewModel)outputResult.ViewData.Model).IsAuthorised);
        }
        [Fact]
        public async Task IndexPost_ReturnsValidView()
        {
            //Arrange
            MskReferral referral = new()
            {
                NhsNumber = "1234"
            };

            MskHubViewModel mskHubauth = new()
            {
                IsAuthorised = true
            };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;


            //Act
            var result = await _classUnderTest.Index(mskHubauth);

            //Assert            
            var outputResult = Assert.IsType<SignOutResult>(result);
            
            Assert.True(outputResult.Properties.RedirectUri == "/");
            Assert.True(outputResult.AuthenticationSchemes.Any());
        }

        [Fact]        
        public async Task ConsentNHSNUmberGP_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() {  };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(),new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.ConsentNhsNumberGP();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            
            Assert.True(((ConsentNHSNumberGPPracticeViewModel)outputResult.ViewData.Model).ConsentYNList.Any());
        }


        [Fact]
        public void ConsentNHSNUmberGPPost_ReturnsInvalidView()
        {
            //Arrange                        
            MskReferral referral = new() { };
            ConsentNHSNumberGPPracticeViewModel model = new() { ConsentToLookups = "false" };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.ConsentNhsNumberGP(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Not-Eligible-For-Service", outputResult.ActionName);
        }
        [Fact]
        public async Task ConsentReferrerUpdate_ReturnsView()
        {
            //Arrange                        
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.ConsentReferrerUpdate();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(((ConsentForReferrerUpdateViewModel)outputResult.ViewData.Model).ConsentYNList.Any());
        }
        [Theory]
        [InlineData("false")]
        [InlineData("true")]
        public void ConsentReferrerUpdatePost_ReturnsView(string consent)
        {
            //Arrange                        
            MskReferral referral = new() { };
            ConsentForReferrerUpdateViewModel model = new() { ConsentToReferrerUpdate = consent };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.ConsentReferrerUpdate(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("nhs-number", outputResult.ActionName);
        }

        [Fact]
        public void NHSNumber_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.NHSNumber();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((NHSNumberViewModel)outputResult.ViewData.Model).NHSNumber);

        }
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("9876543212")]
        [InlineData("98w76543$1")]
        [InlineData("123234 54fd")]
        public async Task NHSNumberPost_ReturnsInvalidView(string nhsnumber)
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            NHSNumberViewModel model = new() { NHSNumber = nhsnumber };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(pharmacyReferral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = await _classUnderTest.NHSNumber(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }
        [Fact]
        public void GPPractice_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.GPPractice();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((GPPracticeViewModel)outputResult.ViewData.Model).ODSCode);

        }
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("12345")]
        [InlineData("123")]
        [InlineData("1 2 4")]
        [InlineData("$1111")]
        [InlineData("$11111")]
        public async Task GPPracticePost_ReturnsRegexInvalidView(string odscode)
        {
            //Arrange                        
            MskReferral referral = new() { };
            GPPracticeViewModel model = new() { ODSCode = odscode };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = await _classUnderTest.GPPractice(model);
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }

        [Theory]
        [InlineData("A1111")]
        [InlineData("B1111")]
        public async Task GPPracticePost_ReturnsAPIInvalidView(string odscode)
        {
            //Arrange                        
            MskReferral referral = new() { };
            GPPracticeViewModel model = new() { ODSCode = odscode };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //mock api
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 404 }));

            //Act
            var result = await _classUnderTest.GPPractice(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }
        [Fact]
        public async Task GPPracticeConfirm_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 200, Status = "Ok" }));

            //Act
            var result = await _classUnderTest.GPPracticeConfirm();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((GPPracticeViewModel)outputResult.ViewData.Model).GPOrg);

        }
        [Fact]
        public void GPPracticeConfirmPost_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { };
            GPPracticeViewModel model = new() { ODSCode = "", GPOrg = new ODSOrganisation() { Name = "" } };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 200, Status = "Ok" }));

            //Act
            var result = _classUnderTest.GPPracticeConfirm(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("osteoarthritis", outputResult.ActionName);

        }

        [Fact]
        public async Task OsteoarthritisGet_ReturnsValidView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new()
            {
                IsAuthorised = true
            };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Osteoarthritis();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(((OsteoarthritisViewModel)outputResult.ViewData.Model).YNList.Any());
        }
        [Fact]
        public async Task OsteoarthritisPost_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            OsteoarthritisViewModel model = new() { };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = await _classUnderTest.Osteoarthritis(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }

        [Fact]
        public async Task OsteoarthritisPost_ReturnsNonEligibleView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            OsteoarthritisViewModel model = new() { ArthritisHip="false", ArthritisKnee="false" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Osteoarthritis(model);

            //Assert         
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Not-Eligible-For-Service", outputResult.ActionName);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 0)]
        [InlineData(-1, null)]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        public async Task HeightImperialPost_ReturnsModelError_HeightNotValid(int? heightft, int? heightin)
        {
            //Arrange            
            HeightImperialViewModel model = new() { HeightFt = heightft, HeightIn = heightin };
            MskReferral referral = new() { Email = "something@nhs.net" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { Email = "something@nhs.net", HeightCm = 100 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.HeightImperial();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((HeightImperialViewModel)outputResult.ViewData.Model).HeightFt);
        }
        [Fact]
        public void HeightGet_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { Email = "something@nhs.net", HeightCm = 120 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

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
            MskReferral referral = new() { Email = "something@nhs.net", HeightCm = heightCm };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net", WeightKg = 120 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

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
            MskReferral referral = new() { Email = "something@nhs.net", WeightKg = weightKg };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Weight(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        [Fact]
        public void WeightImperialGet_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk", WeightKg = 120 };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral").Object;

            //Act
            var result = _classUnderTest.WeightImperial();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(((WeightImperialViewModel)outputResult.ViewData.Model).WeightSt);
        }
        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 0)]
        [InlineData(-1, null)]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        [InlineData(301, 301)]
        public async Task WeightImperialPost_ReturnsModelError_WeightNotValid(int? weightlb, int? weightst)
        {
            //Arrange            
            WeightImperialViewModel model = new() { WeightLb = weightlb, WeightSt = weightst };
            MskReferral referral = new() { Email = "something@nhs.net" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral",mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { Email = "something@nhs.net" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //mock api, api returns 0 items
            _wmsReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            EthnicityViewModel model = new() { ReferralEthnicityGroup = "The patient does not want to disclose their ethnicity" };

            //mock api, api returns 0 items
            _wmsReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { Email = "something@nhs.net" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //mock api
            _wmsReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            WmsReferral.Business.Models.PharmacyReferral referral = new()
            {
                Email = "something@nhs.net",
                Ethnicity = "White",
                ServiceUserEthnicity = "White",
                ServiceUserEthnicityGroup = "White",
                ConsentForGpAndNhsNumberLookup = true
            };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //mock api
            _wmsReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Ethnicity(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("date-of-birth", outputResult.ActionName);
        }

        [Fact]
        public void DateOfBirthGet_ReturnsValidView()
        {
            //Arrange                        
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.DateofBirth();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.Null(((DateOfBirthViewModel)outputResult.ViewData.Model).Day);
        }
        [Fact]
        public async Task DateOfBirthPost_ReturnsModelError_DateNotValid()
        {
            //Arrange            
            DateOfBirthViewModel model = new() { Day = 0, Month = 0, Year = 0 };
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.DateofBirth(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        }

        [Fact]
        public void MobileGet_ReturnsValidView()
        {
            //Arrange                    
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.Mobile();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((MobileViewModel)outputResult.ViewData.Model).Mobile);
        }
        [Theory]
        [InlineData("01123456789")]
        [InlineData("0112345678")]        
        [InlineData("+44(0)7777777777")]        
        [InlineData("+7123456789")]
        [InlineData("-7123456789")]
        [InlineData("01244385052")]
        public async Task MobilePost_ReturnsInvalidView(string mobile)
        {
            //Arrange                        
            MskReferral referral = new() { };
            MobileViewModel model = new() {  Mobile = mobile };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Mobile(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);

        }

        [Theory]
        [InlineData("some")]
        [InlineData("some.some")]
        [InlineData("s.i.f.t")]
        [InlineData("@")]
        [InlineData("some--de@some.com")]
        [InlineData("some.@some.l")]
        public void EmailAddress_ReturnsInvalidView(string email)
        {
            //Arrange                        
            MskReferral referral = new() { };
            EmailAddressViewModel model = new() { Email = email };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);

        }
        [Fact]
        public void FamilyNameGet_ReturnsValidView()
        {
            //Arrange                    
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.FamilyName();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((FamilyNameViewModel)outputResult.ViewData.Model).FamilyName);
        }
        [Theory]
        [InlineData("Familyname")]
        public void FamilyNamePost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            FamilyNameViewModel model = new() { FamilyName = answer };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };
            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.FamilyName(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("given-name", outputResult.ActionName);
        }
        [Fact]
        public void GivenNameGet_ReturnsValidView()
        {
            //Arrange                    
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.GivenName();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((GivenNameViewModel)outputResult.ViewData.Model).GivenName);
        }
        [Theory]
        [InlineData("Givenname")]
        public void GivenNamePost_ReturnsSuccessAction(string answer)
        {
            //Arrange                    
            MskReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            GivenNameViewModel model = new() { GivenName = answer };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.GivenName(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("find-address", outputResult.ActionName);
        }

        [Fact]
        public async Task FindAddressGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { };
            FindAddressViewModel model = new() { Postcode = "" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { Email = "test@test.com", ConsentForGpAndNhsNumberLookup = true };

            //mock api
            _wmsPostCodeioServiceMock.Setup(x => x.ValidPostCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { Email = "test@test.com", Address1 = "Address 1", Postcode = "Z1 1AA" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.Address();

            //Assert 
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Postcode);
            Assert.NotNull(((AddressViewModelV1)outputResult.ViewData.Model).Address1);
        }

        [Fact]
        public async Task TelephoneGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Telephone();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public void TelephonePost_ReturnsModelError()
        {
            //Arrange                    
            MskReferral referral = new() { ConsentForGpAndNhsNumberLookup = true };
            TelephoneViewModel model = new() { Telephone = "" };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            _classUnderTest.ModelState.AddModelError("Telephone", "");
            //Act
            var result = _classUnderTest.Telephone(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);
        }
        [Fact]
        public async Task SexAtBirthGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.Sex();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }

        [Theory]
        [InlineData("Male")]
        [InlineData("Female")]
        public void SexAtBirthPost_ReturnsSuccessAction(string sex)
        {
            //Arrange                    
            MskReferral referral = new() { Email="something@somenhs.nhs.uk" };
            SexViewModel model = new() { Sex = sex };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = _classUnderTest.Sex(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("medical-conditions", outputResult.ActionName);
        }
        [Fact]
        public async Task MedicalConditionsGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new() { };
            MedicalConditionsViewModel model = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.MedicalConditions(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }

        [Fact]
        public async Task LearningDisabilityGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            MskReferral referral = new() { };
            MskHubViewModel mskHubauth = new() { IsAuthorised = false };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.LearningDisability();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }
        [Fact]
        public async Task CheckAnswersGetRefSubmitted_ReturnsAction()
        {
            //Arrange
            MskReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 120,
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
                AnsweredArthritisHip = true,
                AnsweredArthritisKnee = true,
                AnsweredNhsNumberGPConsent = true,
                ReferralSubmitted = true
            };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //Act
            var result = await _classUnderTest.CheckAnswers();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((CheckAnswersViewModel)outputResult.ViewData.Model).KeyAnswer);
        }
        [Fact]
        public async Task CheckAnswersGet_ReturnsView()
        {
            //Arrange
            MskReferral referral = new()
            {
                Email = "test@test.com",
                HeightCm = 120,
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
                AnsweredArthritisHip = true,
                AnsweredArthritisKnee = true,
                AnsweredNhsNumberGPConsent = true
            };

            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

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
            MskReferral referral = new()
            {
                Email = "test@test.com",
                ConsentForGpAndNhsNumberLookup = true,
                HeightCm = 120,
                WeightKg = 100
                
            };
            KeyAnswer keyAnswer = new()
            {
                AnsweredDiabetesType1 = true,
                AnsweredDiabetesType2 = true,
                AnsweredHypertension = true,
                AnsweredLearningDisability = true,                
                AnsweredArthritisHip = true,
                AnsweredArthritisKnee = true
            };
            CheckAnswersViewModel model = new()
            {
                Referral = referral,
                KeyAnswer = keyAnswer
            };
            MskHubViewModel mskHubauth = new() { IsAuthorised = true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeControllerContext(referral, keyAnswer, new ProviderChoiceModel(), "MskReferral", mskHubViewModel: mskHubauth).Object;

            //mock api
            _wmsReferralServiceMock.Setup(x => x.AddMskReferralAsync(It.IsAny<MskReferral>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.NoContent, new StringContent(""))));
            
            //Act
            var result = await _classUnderTest.CheckAnswers(model);

            //Assert            
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("complete", outputResult.ActionName);
        }

        private static List<Ethnicity> GetEthnicities()
        {
            List<Ethnicity> ethnicities = new()
            {
                new Ethnicity { DisplayName = "White", GroupName = "White", TriageName = "White", DisplayOrder = 1, GroupOrder = 1 }
            };
            return ethnicities;
        }
    }
}
