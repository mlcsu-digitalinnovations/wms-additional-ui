using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsPharmacyReferral.Models
{
    public class AuthViewModel
    {
        [Display(Name = "What is your nhs.net email address?")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid nhs.net email address in the correct format, like name@nhs.net"), EmailAddress]
        public string EmailAddress { get; set; }
        [Required(ErrorMessage = "Enter a valid numerical code")]
        [Display(Name = "Enter the code from the email you received")]
        public string Token { get; set; }
        
        [Required(ErrorMessage = "Enter a valid ODS code")]
        [Display(Name = "What is your Pharmacy's ODS code?")]
        public string ODSCode { get; set; }
        public ODSOrganisation ODSOrg { get; set; }
        public bool IsAuthorised { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsTokenGenerated { get; set; }
        public bool IsTokenReRequested { get; set; }
        public string UserTimeZone { get; set; }
    }
}
