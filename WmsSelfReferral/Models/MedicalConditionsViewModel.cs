using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class MedicalConditionsViewModel
    {
        [Display(Name = "Type 1 Diabetes")]
        [Required(ErrorMessage = "Select 'Yes' or 'No' to having Type 1 Diabetes or 'Don't know / Prefer not to say'")]
        public string TypeOneDiabetes { get; set; }

        [Display(Name = "Type 2 Diabetes")]
        [Required(ErrorMessage = "Select 'Yes' or 'No' to having Type 2 Diabetes or 'Don't know / Prefer not to say'")]
        public string TypeTwoDiabetes { get; set; }

        [Display(Name = "Hypertension (High blood pressure)")]
        [Required(ErrorMessage = "Select 'Yes' or 'No' to having Hypertension (High blood pressure) or 'Don't know / Prefer not to say'")]
        public string Hypertension { get; set; }

        [Display(Name = "Arthritis of the knee")]
        [Required(ErrorMessage = "Select 'Yes' or 'No' to having Arthritis of the knee or 'Don't know / Prefer not to say'")]
        public string ArthritisKnee { get; set; }

        [Display(Name = "Arthritis of the hip")]
        [Required(ErrorMessage = "Select 'Yes' or 'No' to having Arthritis of the hip or 'Don't know / Prefer not to say'")]
        public string ArthritisHip { get; set; }

        public IEnumerable<KeyValuePair<string, string>> YNList { get; set; }
    }
}
