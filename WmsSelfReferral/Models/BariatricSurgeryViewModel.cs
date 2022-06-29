using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class BariatricSurgeryViewModel
    {
        [Display(Name = "Have you had bariatric surgery in the last two years?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string BariatricSurgery { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
