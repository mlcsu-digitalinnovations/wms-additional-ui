using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class ConsentForReferrerUpdateViewModel
    {
        [Display(Name = "Do you consent for us to share information with your GP regarding your participation in the programme?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string ConsentToReferrerUpdate { get; set; }
        public IEnumerable<KeyValuePair<string, string>> ConsentYNList { get; set; }
    }
}
