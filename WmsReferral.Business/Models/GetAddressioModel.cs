using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class GetAddressioModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Addresses { get; set; }

    }
}
