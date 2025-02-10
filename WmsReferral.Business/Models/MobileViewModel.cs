using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class MobileViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A UK mobile telephone number is required")]
        [Display(Name = "Mobile phone number")]
        [RegularExpression(Constants.REGEX_MOBILE_PHONE_UK, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a UK mobile telephone number")]
        public string Mobile { get; set; }
    }
}
