using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class FamilyNameViewModel
    {
        [Display(Name = "What is your last name?")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter your last name to continue with your application"), MinLength(1), MaxLength(200)]
        [RegularExpression(Constants.REGEX_FAMILYNAMES, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Name contains invalid characters")]
        public string FamilyName { get; set; }
        public string BackActionRoute { get; set; }
    }
}
