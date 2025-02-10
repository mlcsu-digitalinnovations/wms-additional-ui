using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsPharmacyReferral.Models
{
    public class VulnerablePatientViewModel
    {
        [Required(ErrorMessage = "Select yes if the patient is vulnerable or 'Not Known'")]
        public string VulnerablePatient { get; set; }
        public IEnumerable<KeyValuePair<string, string>> VulnerablePatientList { get; set; }
        [Display(Name = "Can you provide more detail?")]
        public string VulnerableDescription { get; set; }
    }
}
