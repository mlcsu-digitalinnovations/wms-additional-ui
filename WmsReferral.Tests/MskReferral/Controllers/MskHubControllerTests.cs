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
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace WmsReferral.Tests.MskHubTests.Controllers
{
    public class MskHubControllerTests
    {
        private readonly MskHubController _classUnderTest;
        private readonly Mock<IEmailSender> _emailsender;
        private readonly Mock<ILogger<MskHubController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _wmsReferralServiceMock;
        private readonly Mock<IPostcodesioService> _wmsPostCodeioServiceMock;
        private readonly Mock<IODSLookupService> _wmsODSLookupServiceMock;
        private readonly WmsCalculations _referralBusiness;
        private readonly Mock<ILogger<WmsCalculations>> _loggerSelfReferralMock;
        private readonly Mock<IGetAddressioService> _GetAddressioService;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        private readonly Mock<ITempDataDictionary> _tempData;
        private readonly Mock<IUrlHelperFactory> _routing;
        private readonly Mock<IConfiguration> _configuration;

        public MskHubControllerTests()
        {
            _loggerMock = new Mock<ILogger<MskHubController>>();
            _emailsender = new Mock<IEmailSender>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _wmsReferralServiceMock = new Mock<IWmsReferralService>();
            _loggerSelfReferralMock = new Mock<ILogger<WmsCalculations>>();
            _wmsODSLookupServiceMock = new Mock<IODSLookupService>();
            _referralBusiness = new WmsCalculations(_loggerSelfReferralMock.Object);
            _wmsPostCodeioServiceMock = new Mock<IPostcodesioService>();
            _GetAddressioService = new Mock<IGetAddressioService>();
            _configuration = new Mock<IConfiguration>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            _tempData = new Mock<ITempDataDictionary>();
            _routing = new Mock<IUrlHelperFactory>();


            _classUnderTest = new MskHubController(
                _loggerMock.Object,
                _emailsender.Object,
                _wmsReferralServiceMock.Object,                
                _wmsODSLookupServiceMock.Object,
                mockTelemetryClient  ,
                _configuration.Object,
                _httpContextAccessor.Object
                )
            {
                TempData = _tempData.Object
            };





        }
    }
}
