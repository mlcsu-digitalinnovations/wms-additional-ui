using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class EthnicityViewModel
    {
		public string SelectedEthnicity { get; set; }
		[Required(ErrorMessage =
	  "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select the option that best describes your background")]
		public string ReferralEthnicity { get; set; }
		[Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select an ethnic group or 'I do not wish to Disclose my Ethnicity'")]
		public string ReferralEthnicityGroup { get; set; }
		public IEnumerable<KeyValuePair<string, string>> EthnicityList { get; set; }
		public IEnumerable<KeyValuePair<string, string>> EthnicityGroupList { get; set; }
		public string EthnicityGroupDescription { get; set; }
		public string WeightUnits { get; set; } = "Metric";
	}
}
