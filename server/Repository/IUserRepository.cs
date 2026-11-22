using Entities;
namespace Repository
{
    public interface IUserRepository
    {
        Task<User> AddNewUser(User user);
        Task<User?> GetUserById(int id);
        Task<List<User>> GetUsers();
        Task<User?> GetUserByEmail(string email);
        Task<User> Update(int id, User value);
    }
}