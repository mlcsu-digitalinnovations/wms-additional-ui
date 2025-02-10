using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsMskReferral.Models
{
    public class OsteoarthritisViewModel
    {
        [Display(Name = "Osteoarthritis of the knee")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string ArthritisKnee { get; set; } = "";

        [Display(Name = "Osteoarthritis of the hip")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string ArthritisHip { get; set; } = "";

        public IEnumerable<KeyValuePair<string, string>>? YNList { get; set; }
    }
}
