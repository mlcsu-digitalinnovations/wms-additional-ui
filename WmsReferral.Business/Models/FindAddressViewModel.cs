using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class FindAddressViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid English postcode"), MinLength(1), MaxLength(200)]
        [RegularExpression(Constants.REGEX_UKPOSTCODE, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid English postcode")]
        public string Postcode { get; set; }
        public string Address { get; set; }
        public IEnumerable<KeyValuePair<string, string>> AddressList { get; set; }
        public bool? PostCodeValid { get; set; }
    }
}
