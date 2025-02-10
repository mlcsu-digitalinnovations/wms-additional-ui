using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsMskReferral.Models
{
    public class CheckAnswersViewModel
    {
        public MskReferral Referral { get; set; }
        public KeyAnswer KeyAnswer { get; set; }
    }
}
