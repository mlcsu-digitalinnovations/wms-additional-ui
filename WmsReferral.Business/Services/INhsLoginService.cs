using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Services
{
    public interface INhsLoginService
    {
        Task<UserInfoResponse> GetUserInfo(string accessToken);
    }
}
