using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class Provider
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Summary2 { get; set; }
        public string Summary3 { get; set; }
        public string Website { get; set; }
        public string Logo { get; set; }
        public bool IsSelected { get; set; }
    }
}
