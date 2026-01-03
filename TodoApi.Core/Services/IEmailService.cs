using System.Threading.Tasks;

namespace TodoApi.Core.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string userName);
    }
}