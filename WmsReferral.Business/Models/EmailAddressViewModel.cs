using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class EmailAddressViewModel
    {
        [Display(Name = "What is the patient's email address?")]
        [Required (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        [EmailAddress (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        public string Email { get; set; }        
    }
}
