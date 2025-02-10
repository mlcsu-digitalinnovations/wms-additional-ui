using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsSurveys.Web.Helpers;
using Xunit;

namespace WmsReferral.Tests.WmsSurveys.Helpers
{
    public class StaticQuestionnaireHelperTests
    {
        [Fact]
        public void RetrieveAnswers()
        {
            //Arrange               
            var responses = StaticReferralHelper.GetYNList();
            var questionnaire = new QuestionnaireViewModel() { QuestionAnswers = new List<QuestionsViewModel>() };

            //Act
            var questions = StaticQuestionnaireHelper.RetrieveAnswers(1, questionnaire, responses);

            //Assert
            Assert.NotNull(questions);
            Assert.NotNull(questions.Responses);            
        }

        [Fact]
        public void SaveAnswers()
        {
            //Arrange                           
            var questionnaire = new QuestionnaireViewModel() { NotificationKey="ranref2", QuestionAnswers = new List<QuestionsViewModel>() };
            var model = new QuestionsViewModel() { QuestionId = 1, QuestionAresponse = "true" };

            //Act
            var outputresult = StaticQuestionnaireHelper.SaveAnswers(model, questionnaire, 1);

            //Assert
            Assert.NotNull(outputresult);
            Assert.NotNull(outputresult.QuestionAnswers);
        }
    }
}
