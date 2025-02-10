using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class WeightImperialViewModel
    {
        [Display(Name = "Stones")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter weight, including stones")]
        [Range(5, 78, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Stones must be a whole number between 5 and 78")]
        public int? WeightSt { get; set; }
        [Display(Name = "Pounds")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter weight, including pounds")]
        [Range(0.0, 13.99, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Pounds must be a number between 0 and 13")]
        public decimal? WeightLb { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, is required")]
        [Range(1, 31, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, must be a number between 1 and 31")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Day, must be a number between 1 and 31")]
        public int? Day { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, is required")]
        [Range(1, 12, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, must be a number between 1 and 12")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Month, must be a number between 1 and 12")]
        public int? Month { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, is required")]
        [Range(1900, 2100, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, must be a valid year in the format yyyy")]
        [RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>The field Year, must be a number")]
        public int? Year { get; set; }

    }
}
