using WmsElectiveCarePortal.Models;

namespace WmsElectiveCarePortal.Services
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserModel>> Get();
        Task<IEnumerable<UserModel>> GetAll();
        Task<UserModel> Get(string id);
        Task<UserModel> Add(UserModel user);
        Task<UserModel> Update(UserModel user);
        Task<bool> Delete(string id);
        Task<UserModel> InviteUser(UserModel user);
    }
}
