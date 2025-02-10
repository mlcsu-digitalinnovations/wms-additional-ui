using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    
    public class QuestionnaireViewModel
    {
        public string FullName { get; set; } = "";
        public string FamilyName { get; set; } = "";
        public string GivenName { get; set; } = "";
        public string NotificationKey { get; set; } = "";        
        public string QuestionnaireRequested { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string Telephone { get; set; } = "";
        public string Email { get; set; } = "";
        public QuestionnaireType QuestionnaireType { get; set; }
        public QuestionnaireValidationState ValidationState { get; set; }
        public QuestionnaireStatus Status { get; set; } 
        public List<QuestionsViewModel> QuestionAnswers { get; set; }
        public Dictionary<string,string> ApiErrors { get; set; }


    }


    public enum SentimentEnum
    {
        StronglyAgree = 1,
        Agree = 2,
        NeitherAgreeOrDisagree = 3,
        Disagree = 4,
        StronglyDisagree = 5
    }
    public enum ExperienceEnum
    {
        VeryGood = 1,
        Good = 2,
        NeitherGoodNorPoor = 3,
        Poor = 4,
        VeryPoor = 5,
        DoNotKnow = 6
    }

    public enum QuestionnaireStatus
    {
        Created,
        Sending,
        Delivered,
        TemporaryFailure,
        TechnicalFailure,
        PermanentFailure,
        Started,
        Completed
    }
    public enum QuestionnaireValidationState
    {
        NotificationKeyNotFound,
        QuestionnaireTypeIncorrect,
        IncorrectStatus,
        BadRequest,
        Valid,
        Completed,
        NotDelivered,
        Expired
    }
    public enum QuestionnaireType 
    {
        NotSet=-99,
        CompleteProT1,
        CompleteProT2and3,
        CompleteSelfT1,
        CompleteSelfT2and3,
        NotCompleteProT1and2and3,
        NotCompleteSelfT1and2and3
    }

}
