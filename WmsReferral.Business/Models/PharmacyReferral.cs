using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class PharmacyReferral : Referral
    {

        public string ReferringPharmacyODSCode { get; set; }
        public string ReferringPharmacyEmail { get; set; }
        public bool HasRegisteredSeriousMentalIllness { get; set; }        
        [Display(Name = "NHS Number")]
        public string NhsNumber { get; set; }
        [Display(Name = "Does the patient consent that we can look up their NHS number and GP practice name?")]
        public bool? ConsentForGpAndNhsNumberLookup { get; set; }        
        [Display(Name = "Referring GP Practice")]
        public string ReferringGpPracticeName { get; set; }
        [Display(Name = "Referring GP Practice")]
        public string ReferringGpPracticeNumber { get; set; }
        [Display(Name = "Does this person require a phone call to help them get onto the service? ")]
        public bool? IsVulnerable { get; set; }
        public string VulnerableDescription { get; set; }
        public decimal? CalculatedBmiAtRegistration { get; set; }
        [Display(Name = "Does the patient consent to share completion information with the Pharmacy and GP?")]
        public bool? ConsentForReferrerUpdatedWithOutcome { get; set; }

    }
}
