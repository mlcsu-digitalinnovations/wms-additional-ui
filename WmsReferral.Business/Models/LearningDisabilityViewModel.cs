using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class LearningDisabilityViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select yes if you have learning difficulties or 'Prefer not to say'")]
        public string LearningDisability { get; set; }
        public IEnumerable<KeyValuePair<string, string>> LearningDisabilityList { get; set; }
    }
}
