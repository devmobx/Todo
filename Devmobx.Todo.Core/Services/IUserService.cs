using System.Threading.Tasks;
using Devmobx.Todo.Storage.Models.v1;

namespace Devmobx.Todo.Core.Services
{
    public interface IUserService
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(string id);
        Task<User> GetUserByResetTokenAsync(string token);
        string GeneratePasswordResetToken(User user);
        Task UpdatePasswordAsync(User user, string newPassword);
        Task<User> CreateUserAsync(string email, string firstName, string lastName, string password);
        Task SaveUserAsync(User user);  // Add this line
    }
}