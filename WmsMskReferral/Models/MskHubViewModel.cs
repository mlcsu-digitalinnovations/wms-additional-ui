using System.ComponentModel.DataAnnotations;
using WmsReferral.Business.Models;

namespace WmsMskReferral.Models
{
    public class MskHubViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        [EmailAddress(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        public string EmailAddress { get; set; } = "";
        //[Required(ErrorMessage = "Enter a valid ODS code")]
        //[Display(Name = "What is your Organisations ODS code?")]
        public string SelectedMskHub { get; set; } = "";
        public IEnumerable<MskHub> MskHubList { get; set; }
        public string ODSCode { get; set; } = "";
        public ODSOrganisation ODSOrg { get; set; }
        public bool IsAuthorised { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string NameIdentifier { get; set; } = "";
    }
}
