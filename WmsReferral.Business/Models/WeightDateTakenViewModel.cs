using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class WeightDateTakenViewModel
    {
		[Required(ErrorMessage = "Date must include a day")]
		[Range(1, 31, ErrorMessage = "Date must be a real date")]
		public int? Day { get; set; }
		[Required(ErrorMessage = "Date must include a month")]
		[Range(1, 12, ErrorMessage = "Date must be a real date")]
		public int? Month { get; set; }
		[Required(ErrorMessage = "Date must include a year")]
		[Range(1900, 2100, ErrorMessage = "Date must be a real date")]
		public int? Year { get; set; }
	}
}
