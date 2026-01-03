using TodoApi.Core.Services;

namespace TodoApi.App.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                _logger.LogInformation($"Sending password reset email to {email}");

                // TODO: Integrate SendGrid or SMTP
                // For now, just log it
                _logger.LogInformation($"Reset link: {resetLink}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email");
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            try
            {
                _logger.LogInformation($"Sending welcome email to {email} for user {userName}");

                // TODO: Integrate SendGrid or SMTP

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email");
                throw;
            }
        }
    }
}
