using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class AddressViewModelV1
    {
        [Display(Name = "Address line 1")]
        [Required (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter the first line of your address"), MinLength(1), MaxLength(200)]
        public string Address1 { get; set; }
        [Display(Name = "Town or city")]
        public string Address2 { get; set; }
        [Display(Name = "County")]
        public string Address3 { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid English postcode"), MinLength(1), MaxLength(200)]
        [RegularExpression(Constants.REGEX_UKPOSTCODE, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid English postcode")]
        public string Postcode { get; set; }
        public bool? UserWarned { get; set; }
        public bool? PostCodeValid { get; set; }
    }
}
