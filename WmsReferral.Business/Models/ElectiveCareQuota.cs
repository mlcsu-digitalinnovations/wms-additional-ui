using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class ElectiveCareQuota
    {
        public HttpStatusCode Status { get; set; }
        public Dictionary<string, string> Errors { get; set; }
        public ElectiveCareQuotaResult Result { get; set; }
    }

    public class ElectiveCareQuotaResult
    {
        public string OdsCode { get; set; }
        public int QuotaTotal { get; set; }
        public int QuotaRemaining { get; set; }
    }
}
