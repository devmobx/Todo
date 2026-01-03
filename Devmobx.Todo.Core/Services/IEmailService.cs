using System.Threading.Tasks;

namespace Devmobx.Todo.Core.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string userName);
    }
}