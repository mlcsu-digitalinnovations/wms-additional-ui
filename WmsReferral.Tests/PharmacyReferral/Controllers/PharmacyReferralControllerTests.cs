using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WmsPharmacyReferral.Controllers;
using WmsPharmacyReferral.Data;
using WmsPharmacyReferral.Models;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using Xunit;

namespace WmsReferral.Tests.PharmacyReferral.Controllers
{
    public class PharmacyReferralControllerTests
    {
        private readonly PharmacyReferralController _classUnderTest;
        private readonly Mock<IEmailSender> _emailsender;
        private readonly Mock<ILogger<PharmacyReferralController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsPharmacyReferralServiceMock;
        private readonly Mock<IPostcodesioService> _wmsPostCodeioServiceMock;
        private readonly Mock<IODSLookupService> _wmsODSLookupServiceMock;
        private readonly WmsCalculations _pharmacyReferralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        private readonly Mock<IGetAddressioService> _GetAddressioService;
        private readonly Mock<IPharmacyData> _pharmacyData;
        public PharmacyReferralControllerTests()
        {
            _loggerMock = new Mock<ILogger<PharmacyReferralController>>();
            _emailsender = new Mock<IEmailSender>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _wmsPharmacyReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _wmsODSLookupServiceMock = new Mock<IODSLookupService>();
            _pharmacyReferralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsPostCodeioServiceMock = new Mock<IPostcodesioService>();
            _GetAddressioService = new Mock<IGetAddressioService>();
            _pharmacyData = new Mock<IPharmacyData>();
            _classUnderTest = new PharmacyReferralController(
                _loggerMock.Object,
                _emailsender.Object,
                _wmsPharmacyReferralServiceMock.Object,
                _wmsPostCodeioServiceMock.Object,
                _wmsODSLookupServiceMock.Object,
                mockTelemetryClient,                
                _pharmacyReferralBusiness,
                _GetAddressioService.Object,
                _pharmacyData.Object
                );
        }



        [Fact]
        public void Index_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net", IsAuthorised=true };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.Index();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(outputResult.ViewName);
        }
        [Fact]
        public void StartOver_ReturnsView()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.StartOver();

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", outputResult.ActionName);
        }
        [Fact]
        public void Index_ReturnsRedirect()
        {
            //Arrange                        
            AuthViewModel authmodel = new() { EmailAddress = "something@nhs.net" };

            //setup session/context
            _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializeAuthControllerContext(authmodel).Object;

            //Act
            var result = _classUnderTest.Index(authmodel);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", outputResult.ActionName);
        }

        [Theory]
        [InlineData("-1")] //session lost
        [InlineData("-2")] //not authorised
        public void ConsentNHSNUmberGP_ReturnsInvalidView(string referringOdsCode)
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { ReferringPharmacyODSCode = referringOdsCode };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            
            //Act
            var result = _classUnderTest.ConsentNhsNumberGP();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GoneWrong", outputResult.ViewName);
        }


        [Fact]
        public void ConsentNHSNUmberGPPost_ReturnsInvalidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            ConsentNHSNumberGPPracticeViewModel model = new() { ConsentToLookups = "false" };
            
            //setup session/context            
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.ConsentNhsNumberGP(model);
            
            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Not-Eligible-For-Service", outputResult.ActionName);
        }
        [Fact]
        public void ConsentReferrerUpdate_ReturnsView()
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.ConsentReferrerUpdate();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((ConsentForReferrerUpdateViewModel)outputResult.ViewData.Model).ConsentYNList);
        }
        [Theory]
        [InlineData("false")]
        [InlineData("true")]
        public void ConsentReferrerUpdatePost_ReturnsView(string consent)
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            ConsentForReferrerUpdateViewModel model = new() { ConsentToReferrerUpdate = consent };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            GPPracticeViewModel model = new() { ODSCode = odscode };
            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            GPPracticeViewModel model = new() { ODSCode = odscode };
            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 200, Status="Ok" }));

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            GPPracticeViewModel model = new() { ODSCode = "", GPOrg = new ODSOrganisation() {  Name="" } };
            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 200, Status = "Ok" }));

            //Act
            var result = _classUnderTest.GPPracticeConfirm(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("medical-conditions", outputResult.ActionName);

        }
        [Fact]
        public void EmailAddress_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.EmailAddress();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((EmailAddressViewModel)outputResult.ViewData.Model).Email);

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
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            EmailAddressViewModel model = new() { Email = email };
            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.EmailAddress(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.False(outputResult.ViewData.ModelState.IsValid);

        }
        [Fact]
        public void MedicalConditions_ReturnsView()
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.MedicalConditions();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((MedicalConditionsViewModel)outputResult.ViewData.Model).YNList);
        }
        [Theory]        
        [InlineData("true")]
        public void HypertensionPost_ReturnsView(string consent)
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral pharmacyReferral = new() { };
            MedicalConditionsViewModel model = new() { Hypertension = consent, TypeOneDiabetes = consent, TypeTwoDiabetes = consent };
            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(pharmacyReferral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.MedicalConditions(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("height", outputResult.ActionName);
        }
        
        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 0)]
        [InlineData(-1, null)]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        public void HeightImperialPost_ReturnsModelError_HeightNotValid(int? heightft, int? heightin)
        {
            //Arrange            
            HeightImperialViewModel model = new() { HeightFt = heightft, HeightIn = heightin };
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net", HeightCm = 100 };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net", HeightCm = 120 };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
        public void HeightPost_ReturnsInValidView(int? heightCm)
        {
            //Arrange
            HeightViewModel model = new() { Height = heightCm };
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net", HeightCm = heightCm };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Height(model);

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
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
        public void WeightPost_ReturnsInValidView(int? weightKg)
        {
            //Arrange
            WeightViewModel model = new() { Weight = weightKg };
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net", WeightKg = weightKg };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Weight(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        [Fact]
        public void WeightImperialGet_ReturnsValidView()
        {
            //Arrange                        
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk", WeightKg = 120 };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
        public void WeightImperialPost_ReturnsModelError_WeightNotValid(int? weightlb, int? weightst)
        {
            //Arrange            
            WeightImperialViewModel model = new() { WeightLb = weightlb, WeightSt = weightst };
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.WeightImperial(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("WeightError"));
        }
        //[Theory]
        //[InlineData(null, null, null)]
        //[InlineData(0,0,0)]
        //[InlineData(31, 2, 2020)]
        //[InlineData(1, 1, 1020)]
        //public void WeightDateTakenPost_ReturnsModelError_DateNotValid(int? day, int? month, int? year)
        //{
        //    //Arrange            
        //    WeightDateTakenViewModel model = new() { Day = day, Month = month, Year = year };
        //    WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

        //    //setup session/context
        //    _classUnderTest.ControllerContext.HttpContext = TestSetup.InitializePharmacyControllerContext(referral, new KeyAnswer()).Object;

        //    //Act
        //    var result = _classUnderTest.WeightDateTaken(model);

        //    //Assert
        //    var outputResult = Assert.IsType<ViewResult>(result);

        //    Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        //}

        [Fact]
        public async Task EthnicityGroupGet_ReturnsGoneWrongView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net" };

            //mock api, api returns 0 items
            _wmsPharmacyReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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

            EthnicityViewModel model = new() { ReferralEthnicityGroup = "The patient does not want to disclose their ethnicity" };

            //mock api, api returns 0 items
            _wmsPharmacyReferralServiceMock.Setup(x => x.GetEthnicityGroupList(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>() { }));

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhs.net" };

            //mock api
            _wmsPharmacyReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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


            //mock api
            _wmsPharmacyReferralServiceMock.Setup(x => x.GetEthnicities(It.IsAny<string>()))
                .Returns(Task.FromResult((IEnumerable<Ethnicity>)GetEthnicities()));

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.DateofBirth();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.Null(((DateOfBirthViewModel)outputResult.ViewData.Model).Day);
        }
        [Fact]
        public void DateOfBirthPost_ReturnsModelError_DateNotValid()
        {
            //Arrange            
            DateOfBirthViewModel model = new() { Day = 0, Month = 0, Year = 0 };
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.DateofBirth(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);

            Assert.True(outputResult.ViewData.ModelState.ContainsKey("DateError"));
        }

        [Fact]
        public void FamilyNameGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            FamilyNameViewModel model = new() { FamilyName = answer };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

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
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            GivenNameViewModel model = new() { GivenName = answer };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.GivenName(model);

            //Assert
            var outputResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("find-address", outputResult.ActionName);
        }
        [Fact]
        public void AddressGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Address();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((AddressViewModelV1)outputResult.ViewData.Model).Postcode);
        }
        [Fact]        
        public async Task AddressPost_ReturnsSuccessAction()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };
            AddressViewModelV1 model = new() { Postcode = "AB1 2CD" };

            //mock api
            _wmsPostCodeioServiceMock.Setup(x => x.ValidPostCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = await _classUnderTest.Address(model);

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.True(outputResult.ViewData.ModelState.ContainsKey("Postcode"));
        }
        [Fact]
        public void MobileGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Mobile();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((MobileViewModel)outputResult.ViewData.Model).Mobile);
        }
        [Fact]
        public void TelephoneGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Telephone();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((TelephoneViewModel)outputResult.ViewData.Model).Telephone);
        }
        [Fact]
        public void SexGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.Sex();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.Null(((SexViewModel)outputResult.ViewData.Model).Sex);
        }
        [Fact]
        public void PhysicalDisabilityGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.PhysicalDisability();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((PhysicalDisabilityViewModel)outputResult.ViewData.Model).PhysicalDisabilityList);
        }
        [Fact]
        public void LearningDisabilityGet_ReturnsValidView()
        {
            //Arrange                    
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk" };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(new KeyAnswer());

            //Act
            var result = _classUnderTest.LearningDisability();

            //Assert
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((LearningDisabilityViewModel)outputResult.ViewData.Model).LearningDisabilityList);
        }
        
        [Fact]
        public void CheckAnswersGet_ReturnsView()
        {
            //Arrange
            WmsReferral.Business.Models.PharmacyReferral referral = new() { Email = "something@nhsorg.nhs.uk",
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
                AnsweredPatientVulnerable = true
            };

            //setup session/context
            _pharmacyData.Setup(x => x.GetReferralSessionData()).Returns(referral);
            _pharmacyData.Setup(x => x.GetAnswerSessionData()).Returns(keyAnswer);

            //Act
            var result = _classUnderTest.CheckAnswers();

            //Assert            
            var outputResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(((CheckAnswersViewModel)outputResult.ViewData.Model).KeyAnswer);
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
