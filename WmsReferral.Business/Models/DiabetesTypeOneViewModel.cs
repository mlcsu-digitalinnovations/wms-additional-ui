using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class DiabetesViewModel
    {
        [Required (ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select yes if you have a diagnosis of type 1 diabetes or 'Prefer not to say'")]
        public string DiabetesTypeOne { get; set; }
        
        public IEnumerable<KeyValuePair<string, string>> DiabetesList { get; set; }
    }
}
