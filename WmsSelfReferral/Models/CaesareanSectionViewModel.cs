using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class CaesareanSectionViewModel
    {
        [Display(Name = "Have you had a caesarean section in the past 3 months? ")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string CaesareanSection { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
