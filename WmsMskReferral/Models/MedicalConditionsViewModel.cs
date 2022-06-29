using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsMskReferral.Models
{
    public class MedicalConditionsViewModel
    {
        [Display(Name = "Type 1 Diabetes")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string TypeOneDiabetes { get; set; } = "";

        [Display(Name = "Type 2 Diabetes")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string TypeTwoDiabetes { get; set; } = "";

        [Display(Name = "Hypertension (High blood pressure)")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string Hypertension { get; set; } = "";

    

        public IEnumerable<KeyValuePair<string, string>>? YNList { get; set; }
    }
}
