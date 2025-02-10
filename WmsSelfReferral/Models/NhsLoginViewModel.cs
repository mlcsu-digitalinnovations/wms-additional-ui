using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsSelfReferral.Models
{
    public class NhsLoginViewModel
    {
        public string Email { get; set; }
        public bool? Email_verified { get; set; }
        public string Phone { get; set; }
        public bool? Phone_number_verified { get; set; }
        public string Nhs_number { get; set; }
        public string Family_name { get; set; }
        public string Given_name { get; set; }
        public string Gp_ods_code { get; set; }
        public DateTime? Birthdate { get; set; }
        public string Identity_proofing_level { get; set; }
        public Gp_registration_details Gp_registration_details { get; set; }
    }

    public class Gp_registration_details
    {
        public string Gp_ods_code { get; set; }
    }
}
