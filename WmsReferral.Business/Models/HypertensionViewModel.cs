using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class HypertensionViewModel
    {
        [Display(Name = "Hypertension (High blood pressure)")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select yes if you have a diagnosis of hypertension or 'Prefer not to say'")]
        public string Hypertension { get; set; }
        public IEnumerable<KeyValuePair<string, string>> HypertensionList { get; set; }
    }
}
