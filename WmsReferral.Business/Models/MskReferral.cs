using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class MskReferral : Referral
    {

        public string ReferringMskHubOdsCode { get; set; }
        public string ReferringMskClinicianEmailAddress { get; set; }              
        [Display(Name = "NHS Number")]
        public string NhsNumber { get; set; }
        public bool? ConsentForGpAndNhsNumberLookup { get; set; }        
        [Display(Name = "Referring GP Practice")]
        public string ReferringGpPracticeName { get; set; }
        [Display(Name = "Referring GP Practice")]
        public string ReferringGpPracticeNumber { get; set; }        
        [Display(Name = "Arthritis of the knee")]
        public bool? HasArthritisOfKnee { get; set; }
        [Display(Name = "Arthritis of the hip")]
        public bool? HasArthritisOfHip { get; set; }
        public decimal? CalculatedBmiAtRegistration { get; set; }
        public bool? ConsentForReferrerUpdatedWithOutcome { get; set; }
        public string CreatedByUserId { get; set; }

    }
}
