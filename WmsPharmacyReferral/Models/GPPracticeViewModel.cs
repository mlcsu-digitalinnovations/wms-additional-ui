using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsPharmacyReferral.Models
{
    public class GPPracticeViewModel
    {
        [Display(Name = "What is the patient's GP Practice (ODS Code)?")]
        [Required(ErrorMessage = "GP Practice is required")]
        public string ODSCode { get; set; }
        public ODSOrganisation GPOrg { get; set; }
    }
}
