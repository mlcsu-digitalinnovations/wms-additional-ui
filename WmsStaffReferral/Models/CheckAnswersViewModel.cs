using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsStaffReferral.Models
{
    public class CheckAnswersViewModel
    {
        public StaffReferral Referral { get; set; }
        public KeyAnswer KeyAnswer { get; set; }
    }
}
