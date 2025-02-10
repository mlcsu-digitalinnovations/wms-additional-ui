using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Services
{
    public interface IPostcodesioService
    {
        Task<bool> ValidPostCodeAsync(string postcode);
        Task<PostCodesioModel> LookupPostCodeAsync(string postcode);
        
    }
}

