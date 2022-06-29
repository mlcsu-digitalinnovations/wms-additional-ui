using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class StaffRoleViewModel
    {
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Select the staff role that best fits your job")]
        public string StaffRole { get; set; }
        public IEnumerable<KeyValuePair<string, string>> StaffRoleList { get; set; }
    }
}
