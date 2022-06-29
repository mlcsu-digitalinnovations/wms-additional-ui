using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class TelephoneViewModel
    {
        [Display(Name = "Home phone number")]
        [RegularExpression(Constants.REGEX_PHONE_UK, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a UK telephone number")]
        public string Telephone { get; set; }
    }
}
