using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class StaffEmailAddressViewModel
    {
        [Display(Name = "What is your NHS email address?")]
        [Required (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid NHS email address in the correct format")]
        [EmailAddress (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid NHS email address in the correct format")]
        public string Email { get; set; }        
    }
}
