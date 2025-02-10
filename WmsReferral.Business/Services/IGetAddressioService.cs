using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Services
{
    public interface IGetAddressioService
    {

        Task<HttpResponseMessage> FindAddressAsync(string postcode);
        Task<IEnumerable<KeyValuePair<string,string>>> GetAddressList(string postcode);
        
    }
}

