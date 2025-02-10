using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class GivenNameViewModel
    {
        [Display(Name = "What is your first name?")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter your first name to continue with your application"), MinLength(1), MaxLength(200)]
        [RegularExpression(Constants.REGEX_FAMILYNAMES, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Name contains invalid characters")]
        public string GivenName { get; set; }
    }
}
