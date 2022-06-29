using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class PregnantViewModel
    {
        [Display(Name = "Are you pregnant?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string Pregnant { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
