using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class PostCodesioModel
    {
        public int Status { get; set; }
        public PostCodesioResultModel Result { get; set; }
    }

    public class PostCodesioResultModel
    {
        public string Postcode { get; set; }
        public int Quality { get; set; }
        public int? Eastings { get; set; }
        public int? Northings { get; set; }
        public string Country { get; set; }
        public string Nhs_ha { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string European_electoral_region { get; set; }
        public string Primary_care_trust { get; set; }
        public string Region { get; set; }
        public string Lsoa { get; set; }
        public string Msoa { get; set; }
        public string Incode { get; set; }
        public string Outcode { get; set; }
        public string Parliamentary_constituency { get; set; }
        public string Admin_district { get; set; }
        public string Parish { get; set; }
        public object Admin_county { get; set; }
        public string Date_of_introduction { get; set; }
        public string Admin_ward { get; set; }
        public object Ced { get; set; }
        public string Ccg { get; set; }
        public string Nuts { get; set; }
        public string Pfa { get; set; }
        public PostCodesioCodesModel Codes { get; set; }
    }
    

    public class PostCodesioCodesModel
    {
        public string Admin_district { get; set; }
        public string Admin_county { get; set; }
        public string Admin_ward { get; set; }
        public string Parish { get; set; }
        public string Parliamentary_constituency { get; set; }
        public string Ccg { get; set; }
        public string Ccg_id { get; set; }
        public string Ced { get; set; }
        public string Nuts { get; set; }
        public string Lsoa { get; set; }
        public string Msoa { get; set; }
        public string Lau2 { get; set; }
        public string Pfa { get; set; }
    }
}
