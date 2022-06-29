using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class GivenBirthViewModel
    {
        [Display(Name = "Have you given birth less than 3 months ago?")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string GivenBirth { get; set; }
        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
