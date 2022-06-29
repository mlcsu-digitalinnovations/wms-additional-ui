using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;

namespace WmsReferral.Business.Models
{
    public class StaffReferral : Referral
    {
        public string ReferringGpPracticeNumber { get; set; }
        [Required]
        [Display(Name = "What is your main role within the NHS?")]
        public string StaffRole { get; set; }
    }
}
