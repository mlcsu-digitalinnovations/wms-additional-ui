using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsSelfReferral.Data;

using Xunit;

namespace WmsReferral.Tests.SelfReferralTests.Data
{
    public class ReferralSessionDataTests
    {
        private readonly ReferralSessionData _classUnderTest;
        private readonly Mock<ILogger<ReferralSessionData>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IWmsReferralService> _wmsReferralService;

        public ReferralSessionDataTests()
        {
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _loggerMock = new Mock<ILogger<ReferralSessionData>>();
            _wmsReferralService = new Mock<IWmsReferralService>();

            _classUnderTest = new ReferralSessionData
                (
                _loggerMock.Object,
                mockTelemetryClient,
                _httpContextAccessor.Object,
                _wmsReferralService.Object
                );

        }

        [Fact]
        public async Task NotValidSession_Get()
        {
            //Arrange                        
            SelfReferral referral = new() { ConsentForGpAndNhsNumberLookup = null };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            _classUnderTest.SetReferralSessionData(referral);

            //Assert            
            var outputResult = await _classUnderTest.NotValidSession();
            Assert.True(outputResult);

        }

        [Fact]
        public void GetReferralSessionData_Get()
        {
            //Arrange                        
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(new SelfReferral(), new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            var result = _classUnderTest.GetReferralSessionData();

            //Assert
            var outputResult = Assert.IsType<SelfReferral>(result);
            Assert.NotNull(outputResult);
        }

        [Fact]
        public void SetReferralSessionData_Set()
        {
            //Arrange                        
            SelfReferral referral = new() { GivenName = "randomTest" };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(referral, new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            _classUnderTest.SetReferralSessionData(referral);

            //Assert            
            var outputResult = _classUnderTest.GetReferralSessionData();
            Assert.True(outputResult.GivenName == referral.GivenName);
            Assert.NotNull(outputResult);
        }

        

        [Fact]
        public void GetAnswerSessionData_Get()
        {
            //Arrange                        
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(new SelfReferral(), new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            var result = _classUnderTest.GetAnswerSessionData();

            //Assert
            var outputResult = Assert.IsType<KeyAnswer>(result);
            Assert.NotNull(outputResult);
        }

        [Fact]
        public void SetAnswerSessionData_Set()
        {
            //Arrange                        
            KeyAnswer keyanswer = new() {  QueriedReferral = true };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(new SelfReferral(), keyanswer, new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            _classUnderTest.SetAnswerSessionData(keyanswer);

            //Assert            
            var outputResult = _classUnderTest.GetAnswerSessionData();
            Assert.True(outputResult.QueriedReferral == keyanswer.QueriedReferral);
            Assert.NotNull(outputResult);
        }

        [Fact]
        public void GetProviderChoiceSessionData_Get()
        {
            //Arrange                        
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(new SelfReferral(), new KeyAnswer(), new ProviderChoiceModel(), "SelfReferral").Object);
            //Act
            var result = _classUnderTest.GetProviderChoiceSessionData();

            //Assert
            var outputResult = Assert.IsType<ProviderChoiceModel>(result);
            Assert.NotNull(outputResult);
        }

        [Fact]
        public void SetProviderChoiceSessionData_Set()
        {
            //Arrange                        
            ProviderChoiceModel choice = new() { Token = "MyProvider" };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeControllerContext(new SelfReferral(), new KeyAnswer(), choice, "SelfReferral").Object);
            //Act
            _classUnderTest.SetProviderChoiceSessionData(choice);

            //Assert            
            var outputResult = _classUnderTest.GetProviderChoiceSessionData();
            Assert.True(outputResult.Token == choice.Token);
            Assert.NotNull(outputResult);
        }
    }
}
