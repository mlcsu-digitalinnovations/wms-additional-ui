﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsPharmacyReferral.Models
{
    public class ConsentNHSNumberGPPracticeViewModel
    {
        [Display(Name = "Does the patient agree that we can look up their NHS number and GP practice name? ")]
        [Required(ErrorMessage = "An answer must be provided")]
        public string ConsentToLookups { get; set; }
        public IEnumerable<KeyValuePair<string, string>> ConsentYNList { get; set; }
    }
}
