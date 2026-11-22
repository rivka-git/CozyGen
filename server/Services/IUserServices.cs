using DTOs;
using Entities;

namespace Services
{
    public interface IUserServices
    {
        Task<DtoAuthResponse?> AddNewUser(DtoUser_All user);
        Task<DtoUser_Name_Gmail_Role_Id?> GetUserById(int id);
        Task<IEnumerable<User>> GetUsers();
        Task<DtoAuthResponse?> Login(DtoUser_Gmail_Password value);
        Task<DtoUser_Name_Gmail_Role_Id> Update(int id, DtoUser_All value);
    }
}
