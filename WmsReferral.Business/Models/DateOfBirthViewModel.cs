using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class DateOfBirthViewModel
    {
		[Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth must include a day")]
		[Range(1, 31, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Day must be a valid number between 1 and 31")]
		[RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Day must be a valid number between 1 and 31")]
		public int? Day { get; set; }
		[Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth must include a month")]
		[Range(1, 12, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Month must be a valid number between 1 and 12")]
		[RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Month must be a valid number between 1 and 12")]
		public int? Month { get; set; }
		[Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth must include a year")]
		[Range(1900, 2100, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Year must be a valid number")]
		[RegularExpression(Constants.REGEX_NUMERICS, ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Date of birth, Year must be a valid number")]
		public int? Year { get; set; }
		public string BackActionRoute { get; set; }
		public bool IsSourceNhslogin { get; set; }
	}
}
