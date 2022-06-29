using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class ProviderChoiceModel
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A service must be selected")]
        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }
        public List<Provider> ProviderChoices { get; set; }
        public string Ubrn { get; set; }
        public string Token { get; set; }
    }
}
