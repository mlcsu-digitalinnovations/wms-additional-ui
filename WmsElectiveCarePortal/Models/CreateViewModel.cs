using System.ComponentModel.DataAnnotations;

namespace WmsElectiveCarePortal.Models
{
    public class CreateViewModel
    {
        [Display(Name = "Email address")]
        [EmailAddress]
        [Required]
        public string EmailAddress { get; set; }

        [Display(Name = "Redemption method")]
        [Required]
        public string RedemptionMethod { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set;}
        public string OdsCode { get; set;}
    }
}
