using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class SelfReferral : Referral
    {
        public string Id { get; set; }        
        [Display(Name = "NHS Number")]
        public string NhsNumber { get; set; }
        public bool? ConsentForGpAndNhsNumberLookup { get; set; }        
        [Display(Name = "Referring GP Practice")]
        public string ReferringGpPracticeNumber { get; set; }                      
        public bool? ConsentForReferrerUpdatedWithOutcome { get; set; }
        [Display(Name = "Have had bariatric surgery in the last two years? ")]
        public bool? HasHadBariatricSurgery { get; set; }
        [Display(Name = "Do you have an active eating disorder?")]
        public bool? HasActiveEatingDisorder { get; set; }
        [Display(Name = "Are you pregnant?")]
        public bool? IsPregnant { get; set; }       
        [Display(Name = "Arthritis of the knee")]
        public bool? HasArthritisOfKnee { get; set; }
        [Display(Name = "Arthritis of the hip")]
        public bool? HasArthritisOfHip { get; set; }        
        public string NhsLoginClaimFamilyName { get; set; }
        public string NhsLoginClaimGivenName { get; set; }
        public string NhsLoginClaimMobile { get; set; }
        public string NhsLoginClaimEmail { get; set; }
        public string LastSubmitEmail { get; set; }
        public string LastSubmitFamilyName { get; set; }
        public string LastSubmitGivenName { get; set; }
        public string LastSubmitMobile { get; set; }        
        public bool? IsDateOfBmiAtRegistrationValid { get; set; }
        public string ReferralSource { get; set; }
    }
}
