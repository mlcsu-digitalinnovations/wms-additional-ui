using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class HeightImperialViewModel
    {
        [Display(Name = "Feet")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter height including feet")]
        [Range(1, 8, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field feet must be a number between 1 and 8")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field feet must be a number between 1 and 8")]
        public int? HeightFt { get; set; }
        [Display(Name = "Inches")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter height including inches")]
        [Range(0.0, 11.99, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field inches must be a number between 0 and 11")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field inches must be a number between 0 and 11")]
        public decimal? HeightIn { get; set; }

    }
}
