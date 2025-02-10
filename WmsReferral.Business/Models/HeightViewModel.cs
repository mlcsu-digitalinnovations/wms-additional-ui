using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class HeightViewModel
    {
        [Display(Name = "Centimeters")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter height in centimetres")]
        [Range(50, 250, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Height, in centimetres must be between 50 and 250.")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Height, in centimetres must be a number.")]
        public decimal? Height { get; set; }
        
    }
}
