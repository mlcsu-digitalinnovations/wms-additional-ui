using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsMskReferral.Models
{
    public class NHSNumberViewModel
    {
        [Display(Name = "What is the patient's NHS number?")]
        [Required(ErrorMessage = "NHS Number is required")]
        public string NHSNumber { get; set; } = "";
    }
}
