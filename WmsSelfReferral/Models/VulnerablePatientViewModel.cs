using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class VulnerabilitiesViewModel
    {
        [Required(ErrorMessage = "Do you have any vulnerabilities?")]
        public string HasVulnerabilities { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
        [Display(Name = "If yes, please briefly describe your vulnerability and additional needs?")]
        public string VulnerabilityDescription { get; set; }
    }
}
