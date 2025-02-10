using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class PhysicalDisabilityViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select yes if you have a physical disability or 'Prefer not to say'")]
        public string PhysicalDisability { get; set; }
        public IEnumerable<KeyValuePair<string, string>> PhysicalDisabilityList { get; set; }
    }
}
