using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class Ethnicity
    {
        public string DisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public string GroupName { get; set; }
        public int GroupOrder { get; set; }
        public string TriageName { get; set; }
    }
}
