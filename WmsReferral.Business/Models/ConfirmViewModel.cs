using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class ConfirmViewModel
    {
        [Required(ErrorMessage = "You must confirm")]
        public string Confirmation { get; set; }
    }
}
