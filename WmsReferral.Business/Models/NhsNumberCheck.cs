using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class NhsNumberCheck
    {
        
        public SelfReferral Referral { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string> Errors { get; set; }
    }
}
