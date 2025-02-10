using System.ComponentModel.DataAnnotations;

namespace WmsStaffReferral.Models
{
    public class OTPAuthViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        [EmailAddress(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Enter a valid email address in the correct format")]
        public string EmailAddress { get; set; }        
        public bool IsAuthorised { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string NameIdentifier { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserTimeZone { get; set; } = string.Empty;
        public bool IsTokenGenerated { get; set; } = false;
        public bool IsTokenReRequested { get; set; } = false;
    }
}
