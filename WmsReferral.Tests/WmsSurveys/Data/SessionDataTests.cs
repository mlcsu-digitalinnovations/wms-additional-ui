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
using WmsSurveys.Web.Data;
using Xunit;

namespace WmsReferral.Tests.WmsSurveys.Data
{
    public class SessionDataTests
    {
        private readonly QuestionnaireData _classUnderTest;
        private readonly Mock<ILogger<QuestionnaireData>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IWmsReferralService> _wmsReferralService;
        public SessionDataTests()
        {                       
            _httpContextAccessor = new Mock<IHttpContextAccessor>();  
            TelemetryClient mockTelemetryClient = TestSetup.InitializeMockTelemetryChannel();
            _loggerMock = new Mock<ILogger<QuestionnaireData>>();
            _wmsReferralService = new Mock<IWmsReferralService>();
            
            _classUnderTest = new QuestionnaireData
                (
                _loggerMock.Object, 
                mockTelemetryClient, 
                _httpContextAccessor.Object, 
                _wmsReferralService.Object
                );

        }

        [Fact]
        public void SessionData_Get()
        {
            //Arrange                        
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(new QuestionnaireViewModel()).Object);
            //Act
            var result = _classUnderTest.GetSessionData();

            //Assert
            var outputResult = Assert.IsType<QuestionnaireViewModel>(result);
            Assert.NotNull(outputResult);
        }

        [Fact]
        public void SessionData_Set()
        {
            //Arrange                        
            QuestionnaireViewModel questionnaire = new() { FullName = "randomTest" };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(new QuestionnaireViewModel() { FullName="randomTest" }).Object);
            //Act
            _classUnderTest.SetSessionData(questionnaire);

            //Assert            
            var outputResult = _classUnderTest.GetSessionData();
            Assert.True(outputResult.FullName == questionnaire.FullName);            
            Assert.NotNull(outputResult);
        }
        [Fact]
        public void SessionData_End()
        {
            //Arrange                        
            
            //Act
            _classUnderTest.EndSession();

            //Assert
            var outputResult = _classUnderTest.GetSessionData();
            Assert.True(outputResult.FamilyName=="");            
            Assert.NotNull(outputResult);
        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task GetQuestionnaire_EmptyResponse(string notificationKey) 
        {
            //Arrange                        
            QuestionnaireViewModel questionnaire = new() { FullName = "randomTest", NotificationKey="none" };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(questionnaire).Object);

            _wmsReferralService
                .Setup(x => x.GetQuestionnaireAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(""))));

            //Act
            var outputResult = await _classUnderTest.GetQuestionnaire(notificationKey);

            //Assert (API returned OK 200, but response empty)          
            Assert.True(outputResult.Status == QuestionnaireStatus.TemporaryFailure);
            Assert.NotNull(outputResult);

        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task GetQuestionnaire_KeyChanged(string notificationKey)
        {
            //Arrange                        
            QuestionnaireViewModel questionnaire = new() { NotificationKey = "none" };
            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(questionnaire).Object);

            _wmsReferralService
                .Setup(x => x.GetQuestionnaireAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent("{\"givenName\":\"Test\",\"familyName\":\"Test\",\"questionnaireType\":\"CompleteProT1\",\"providerName\":\"\" }"))));

            //Act
            var outputResult = await _classUnderTest.GetQuestionnaire(notificationKey);

            //Assert (Key changed)            
            Assert.True(outputResult.Status == QuestionnaireStatus.Started);
            Assert.True(outputResult.NotificationKey == notificationKey);
            Assert.NotNull(outputResult);

        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task CompleteQuestionnaire_NotComplete(string notificationKey)
        {
            //Arrange                        
            QuestionnaireViewModel questionnaire = new() { NotificationKey = notificationKey,
                QuestionAnswers = new List<QuestionsViewModel>()
                {
                    new QuestionsViewModel() { QuestionId=1 }                 
                }
            };

            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(questionnaire).Object);

            //_wmsReferralService
            //    .Setup(x => x.GetQuestionnaireAsync(It.IsAny<string>()))
            //    .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent("{\"givenName\":\"Test\",\"familyName\":\"Test\",\"questionnaireType\":\"CompleteProT1\",\"providerName\":\"\" }"))));

            //Act
            var outputResult = await _classUnderTest.CompleteQuestionnaire(notificationKey);

            //Assert          
            Assert.True(outputResult.Status == QuestionnaireStatus.TechnicalFailure);            
            Assert.NotNull(outputResult);
        }

        [Theory]
        [InlineData("k4jgj4jsixk66")]
        public async Task CompleteQuestionnaire_Complete(string notificationKey)
        {
            //Arrange                        
            QuestionnaireViewModel questionnaire = new()
            {
                NotificationKey = notificationKey,
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

            _httpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(TestSetup.InitializeQuestionnaireContext(questionnaire).Object);

            _wmsReferralService
                .Setup(x => x.CompleteQuestionnaireAsync(It.IsAny<QuestionnaireViewModel>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(TestSetup.HttpResponseMessageMock(HttpStatusCode.OK, new StringContent(""))));
           
            //Act
            var outputResult = await _classUnderTest.CompleteQuestionnaire(notificationKey);

            //Assert          
            Assert.True(outputResult.Status == QuestionnaireStatus.Completed);
            Assert.NotNull(outputResult);
        }

    }
}
