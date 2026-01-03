using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Devmobx.Todo.Core.Services;
using Devmobx.Todo.Storage;
using Devmobx.Todo.Storage.Models.v1;

namespace Devmobx.Todo.App.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly DataBaseContext _dbContext;

        public UserService(ILogger<UserService> logger, DataBaseContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _dbContext.UserV1
                    .FirstOrDefaultAsync(u => u.Email == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email");
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            try
            {
                return await _dbContext.UserV1.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id");
                throw;
            }
        }

        public async Task<User> GetUserByResetTokenAsync(string token)
        {
            try
            {
                var user = await _dbContext.UserV1
                    .FirstOrDefaultAsync(u => u.ResetToken == token);

                if (user?.ResetTokenExpiry < DateTime.UtcNow)
                    return null;

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by reset token");
                throw;
            }
        }

        public string GeneratePasswordResetToken(User user)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24);
            return token;
        }

        public async Task UpdatePasswordAsync(User user, string newPassword)
        {
            try
            {
                user.PasswordHash = HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                await SaveUserAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password");
                throw;
            }
        }

        public async Task<User> CreateUserAsync(string email, string firstName, string lastName, string password)
        {
            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email.ToLower(),
                    FirstName = firstName,
                    LastName = lastName,
                    PasswordHash = HashPassword(password),
                    IsEmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.UserV1.Add(user);
                await _dbContext.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task SaveUserAsync(User user)
        {
            try
            {
                _dbContext.UserV1.Update(user);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user");
                throw;
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var salt = RandomNumberGenerator.GetBytes(16);
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));
                return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            var parts = hash.Split(':');
            if (parts.Length != 2) return false;

            var salt = parts[0];
            using (var sha256 = SHA256.Create())
            {
                var newHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + salt));
                return Convert.ToBase64String(newHash) == parts[1];
            }
        }
    }
}
