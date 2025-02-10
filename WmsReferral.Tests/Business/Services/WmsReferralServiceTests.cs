using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WmsReferral.Business.Services;
using Xunit;

namespace WmsReferral.Tests.Business.Services;

public class WmsReferralServiceTests
{
  private readonly HttpClient _httpClient;
  private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
  private readonly Mock<ILogger<WmsReferralService>> _loggerMock;
  private readonly IWmsReferralService _serviceToTest;
  private const string ValidLinkId = "testlinkid01";

  public WmsReferralServiceTests()
  {
    Dictionary<string, string> configSettings = new()
    {
      { "WmsReferral:apiBaseAddress", "https://localtest.com" },
      { "WmsReferral:apiEndPoint", "/generalreferral" },
      { "WmsReferral:apiKey", Guid.NewGuid().ToString() }
    };
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(configSettings)
      .Build();

    _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
    _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
    {
      BaseAddress = new Uri("http://test.com/")
    };
    _loggerMock = new Mock<ILogger<WmsReferralService>>();
    TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
    _serviceToTest = new WmsReferralService(_httpClient, configuration, mockTelemetryClient, _loggerMock.Object);
  }

  [Fact]
  public async Task ValidateLinkId_ReferralApiReturns200OK_ReturnsTrue()
  {
    // Arrange.
    SetHttpMessageHttpHandlerMock(HttpStatusCode.OK);

    // Act.
    bool result = await _serviceToTest.ValidateLinkId(ValidLinkId);

    // Assert.
    Assert.True(result);
  }

  [Theory]
  [InlineData(HttpStatusCode.BadRequest)]
  [InlineData(HttpStatusCode.Forbidden)]
  [InlineData(HttpStatusCode.InternalServerError)]
  [InlineData(HttpStatusCode.NotFound)]
  [InlineData(HttpStatusCode.Unauthorized)]
  public async Task ValidateLinkId_ReferralApiReturnsOtherStatusCode_ReturnsFalse(
    HttpStatusCode statusCode)
  {
    // Arrange.
    SetHttpMessageHttpHandlerMock(statusCode);

    // Act.
    bool result = await _serviceToTest.ValidateLinkId(ValidLinkId);

    // Assert.
    Assert.False(result);
  }

  [Fact]
  public async Task ValidateLinkId_ReferralApiThrowsException_ReturnsFalse()
  {
    // Arrange.
    SetHttpMessageHttpHandlerMockException("Test Exception");

    // Act.
    bool result = await _serviceToTest.ValidateLinkId(ValidLinkId);

    // Assert.
    Assert.False(result);
  }

  private void SetHttpMessageHttpHandlerMock(HttpStatusCode statusCode)
  {
    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
      "SendAsync",
      ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.AbsolutePath
        .Contains("/ValidateLinkId")),
      ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage
      {
        StatusCode = statusCode
      });
  }

  private void SetHttpMessageHttpHandlerMockException(string message)
  {
    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
      "SendAsync",
      ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.AbsolutePath
        .Contains("/ValidateLinkId/")),
      ItExpr.IsAny<CancellationToken>())
      .ThrowsAsync(new Exception(message));
  }
}
