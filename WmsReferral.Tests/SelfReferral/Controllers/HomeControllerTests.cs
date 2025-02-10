using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;
using WmsSelfReferral.Models;
using WmsSelfReferral.Controllers;
using WmsSelfReferral.Helpers;
using Xunit;

namespace WmsReferral.Tests.SelfReferralTests.Controllers;

public class HomeControllerTests
{
  private readonly HomeController _classUnderTest;
  private readonly Mock<ILogger<HomeController>> _loggerMock;
  private readonly Mock<ITempDataDictionary> _tempData;

  public HomeControllerTests()
  {
    _loggerMock = new Mock<ILogger<HomeController>>();
    _tempData = new Mock<ITempDataDictionary>();
    TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();

    _classUnderTest = new(_loggerMock.Object, mockTelemetryClient)
    {
      TempData = _tempData.Object
    };
  }

  [Fact]
  public void Index_LinkIdIsInvalid_ReturnsGoneWrong()
  {
    // Arrange.
    string linkId = "testid012345";

    // Act.
    IActionResult result = _classUnderTest.Index(linkId);

    // Assert.
    ViewResult outputResult = Assert.IsType<ViewResult>(result);
    Assert.True(outputResult.ViewName == "GoneWrong");
    Assert.True(((ErrorViewModel)outputResult.ViewData.Model).Message == "Error: User link ID is invalid.");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData(" ")]
  public void Index_LinkIdIsNullOrWhiteSpace_ReturnsIndex(string linkId)
  {
    // Arrange.

    // Act.
    IActionResult result = _classUnderTest.Index(linkId);

    // Assert.
    ViewResult outputResult = Assert.IsType<ViewResult>(result);
    HomeIndexViewModel outputResultModel = Assert.IsType<HomeIndexViewModel>(outputResult.Model);
    Assert.True(outputResultModel.UserLinkIdIsNullOrWhiteSpace);
  }

  [Fact]
  public void Index_ReturnsValidView()
  {
    // Arrange.
    string linkId = "testid999999";
    Mock<HttpContext> mockContext = new();
    mockContext.Setup(x => x.User.Identity.IsAuthenticated).Returns(false);
    _classUnderTest.ControllerContext.HttpContext = mockContext.Object;
    TempDataDictionary tempData = new(_classUnderTest.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    _classUnderTest.TempData = tempData;

    // Act.
    IActionResult result = _classUnderTest.Index(linkId);

    // Assert.
    string tempDataLinkId = _classUnderTest.TempData[ControllerConstants.TEMPDATA_LINK_ID] as string;
    ViewResult outputResult = Assert.IsType<ViewResult>(result);
    Assert.True(tempDataLinkId == linkId);
  }
}
