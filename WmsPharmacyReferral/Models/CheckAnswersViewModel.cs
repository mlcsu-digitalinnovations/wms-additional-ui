using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsPharmacyReferral.Models
{
    public class CheckAnswersViewModel
    {
        public PharmacyReferral Referral { get; set; }
        public KeyAnswer KeyAnswer { get; set; }
    }
}
