using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsSelfReferral.Models
{
    public class CheckAnswersViewModel
    {
        public SelfReferral Referral { get; set; }
        public KeyAnswer KeyAnswer { get; set; }
        public NhsLoginViewModel NhsLogin { get; set; }
    }
}
