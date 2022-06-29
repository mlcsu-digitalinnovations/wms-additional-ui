using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class ODSOrganisation
    {
        public int APIStatusCode { get; set; }
        public string Name { get; set; }
        public string AddrLn1 { get; set; }
        public string AddrLn2 { get; set; }
        public string PostCode { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Status { get; set; }
    }
}
