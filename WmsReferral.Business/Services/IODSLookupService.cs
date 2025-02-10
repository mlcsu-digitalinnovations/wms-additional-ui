using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Models;

namespace WmsReferral.Business.Services
{
    public interface IODSLookupService
    {
        //Task<bool> ValidPostCodeAsync(string postcode);
        Task<ODSOrganisation> LookupODSCodeAsync(string ODSCode);
    }
}
