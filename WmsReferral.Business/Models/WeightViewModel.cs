using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class WeightViewModel
    {
        [Display(Name = "Weight, in kilograms")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter weight in kilograms")]
        [Range(35, 500, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Weight, must be a valid number between 35 and 500")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Weight, in kilograms must be a number.")]
        public decimal? Weight { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, is required")]
        [Range(1, 31, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, must be a number between 1 and 31")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, must be a number between 1 and 31")]
        public int? Day { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, is required")]
        [Range(1, 12, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, must be a number between 1 and 12")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, must be a number between 1 and 12")]
        public int? Month { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, is required")]
        [Range(1900, 2100, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, must be a valid year in the format yyyy ")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, must be a number")]
        public int? Year { get; set; }
    }
}
