using System.ComponentModel.DataAnnotations;
using WmsReferral.Business.Models;

namespace WmsMskReferral.Models
{
    public class MskHubViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        [EmailAddress(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        public string? EmailAddress { get; set; }
        //[Required(ErrorMessage = "Enter a valid ODS code")]
        //[Display(Name = "What is your Organisations ODS code?")]
        [Required(ErrorMessage = "Please enter your MSK site.")]
        public string SelectedMskHub { get; set; } = string.Empty;
        public IEnumerable<MskHub>? MskHubList { get; set; }
        public string ODSCode { get; set; } = string.Empty;
        public ODSOrganisation? ODSOrg { get; set; }
        public bool IsAuthorised { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string NameIdentifier { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserTimeZone { get; set; } = string.Empty;
        public bool IsTokenGenerated { get; set; } = false;
        public bool IsTokenReRequested { get; set; } = false;

    }
}
