//using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class ConsentFutureContactViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select yes if you are happy to be contacted by NHS England in future")]
        public string FutureContact { get; set; }
        public IEnumerable<KeyValuePair<string, string>> FutureContactList { get; set; }
    }
}
