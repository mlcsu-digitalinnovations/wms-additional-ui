using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Models;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using WmsStaffReferral.Controllers;
using WmsSurveys.Web.Controllers;
using WmsSurveys.Web.Data;
using Xunit;

namespace WmsReferral.Tests.WmsSurveys.Controllers
{
    public class QuestionnaireControllerTests
    {
        private readonly QuestionnaireController _classUnderTest;
        private readonly Mock<ILogger<QuestionnaireController>> _loggerMock;
        private readonly Mock<IWmsReferralService> _WmsServiceMock;
        private readonly Mock<IQuestionnaireData> _questionnaireData;

        public QuestionnaireControllerTests()
        {
            _loggerMock = new Mock<ILogger<QuestionnaireController>>();
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _WmsServiceMock = new Mock<IWmsReferralService>();
            _questionnaireData = new Mock<IQuestionnaireData>();


            _classUnderTest = new QuestionnaireController(
                _loggerMock.Object,
                _WmsServiceMock.Object,
                mockTelemetryClient,
                _questionnaireData.Object
                );
        }


        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task QuestionnaireRequest_ReturnsRedirect(string notificationKey)
        {
            //Arrange            
            QuestionnaireViewModel questionnaire = new()
            {
                NotificationKey = notificationKey,
                QuestionnaireType = QuestionnaireType.CompleteProT1,
                QuestionnaireRequested = "cpl1",
                Status = QuestionnaireStatus.Started
            };

            //mock sessiondata
            _questionnaireData.Setup(x => x.GetSessionData()).Returns(questionnaire);
            _questionnaireData.Setup(x => x.GetQuestionnaire(It.IsAny<string>())).Returns(Task.FromResult(questionnaire));

            //Act
            var result = await _classUnderTest.QuestionnaireRequest(notificationKey);

            //Assert
            var outputResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/" + questionnaire.QuestionnaireRequested + "/" + notificationKey + "/begin", outputResult.Url);
        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task QuestionnaireComplete_ReturnsCompleteView(string notificationKey)
        {
            //Arrange            
            QuestionnaireViewModel questionnaire = new()
            {
                NotificationKey = notificationKey,
                QuestionnaireType = QuestionnaireType.CompleteProT1,
                QuestionnaireRequested = "cpl1",
                Status = QuestionnaireStatus.Completed,
                QuestionAnswers = new List<QuestionsViewModel>()
                {
                    new QuestionsViewModel() { QuestionId=1 },
                    new QuestionsViewModel() { QuestionId=2 },
                    new QuestionsViewModel() { QuestionId=3 },
                    new QuestionsViewModel() { QuestionId=4 },
                    new QuestionsViewModel() { QuestionId=5 },
                    new QuestionsViewModel() { QuestionId=6 }
                }
            };

            //mock sessiondata
            _questionnaireData.Setup(x => x.GetSessionData()).Returns(questionnaire);
            _questionnaireData.Setup(x => x.CompleteQuestionnaire(It.IsAny<string>())).Returns(Task.FromResult(questionnaire));
            //mock api
            _WmsServiceMock.Setup(x => x.CompleteQuestionnaireAsync(It.IsAny<QuestionnaireViewModel>(),It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(""))));

            //Act
            var result = await _classUnderTest.Complete(notificationKey);

            //Assert
            Assert.IsType<ViewResult>(result);            
        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task QuestionnaireComplete_ReturnsErrorRedirect(string notificationKey)
        {
            //Arrange            
            QuestionnaireViewModel questionnaire = new()
            {
                NotificationKey = notificationKey,
                QuestionnaireType = QuestionnaireType.CompleteProT1,
                QuestionnaireRequested = "cpl1",
                Status = QuestionnaireStatus.TechnicalFailure,
                QuestionAnswers = new List<QuestionsViewModel>()
            };

            //mock sessiondata
            _questionnaireData.Setup(x => x.GetSessionData()).Returns(questionnaire);
            _questionnaireData.Setup(x => x.CompleteQuestionnaire(It.IsAny<string>())).Returns(Task.FromResult(questionnaire));

            //Act
            var result = await _classUnderTest.Complete(notificationKey); 

            //Assert
            var outputResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/u/" + notificationKey + "/error", outputResult.Url);
        }

    }
}
