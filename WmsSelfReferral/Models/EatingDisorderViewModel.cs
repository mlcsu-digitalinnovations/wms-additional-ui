using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class EatingDisorderViewModel
    {
        [Display(Name = "Do you have an active eating disorder?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string EatingDisorder { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
