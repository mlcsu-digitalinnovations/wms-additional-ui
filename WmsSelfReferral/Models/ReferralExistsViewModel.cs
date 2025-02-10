using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class ReferralExistsViewModel
    {
        public Dictionary<string, List<string>> Errors {get;set;}
        public string ErrorDescription { get; set; }
        public string ChosenProvider { get; set; }
        public DateTime? DateOfReferral { get; set; }
        public string Name { get; set; }
    }
}
