using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class SexViewModel
    {
        
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select if you are female or male")]
        public string Sex { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Sexes { get; set; }
    }
}
