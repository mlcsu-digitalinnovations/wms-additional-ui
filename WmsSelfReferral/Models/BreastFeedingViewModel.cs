using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class BreastFeedingViewModel
    {
        [Display(Name = "Are you breast feeding?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string BreastFeeding { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
