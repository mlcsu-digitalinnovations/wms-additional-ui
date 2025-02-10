using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsMskReferral.Models
{
    public class ConsentForReferrerUpdateViewModel
    {
        [Display(Name = "Does the patient consent to share their completion information with MSK and GP?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string ConsentToReferrerUpdate { get; set; } = "";
        public IEnumerable<KeyValuePair<string, string>>? ConsentYNList { get; set; }
    }
}
