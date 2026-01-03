using Microsoft.AspNetCore.Mvc;
using TodoApi.Core.Services;
using TodoApi.App.Services;
using TodoApi.Storage.Models.v1.Auth;
using TodoApi.Storage.Models.v1;
using TodoApi.Core.Attributes;

namespace TodoApi.App.Controllers
{
    [ApiController]
    [ApiRoute("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if user exists
                var existingUser = await _userService.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "Email already registered" });

                // Create user
                var user = await _userService.CreateUserAsync(
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    request.Password
                );

                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "Registration failed" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Get user by email
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                // Verify password
                if (!UserService.VerifyPassword(request.Password, user.PasswordHash))
                    return Unauthorized(new { message = "Invalid email or password" });

                // Generate JWT token (or use session)
                var token = GenerateJwtToken(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Login failed" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                    return Ok(new { message = "If email exists, reset link has been sent" });

                var resetToken = _userService.GeneratePasswordResetToken(user);
                await _userService.SaveUserAsync(user);

                var resetLink = $"https://localhost:3000/reset-password?token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                return Ok(new { message = "Reset link sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                return StatusCode(500, new { message = "Error processing request" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _userService.GetUserByResetTokenAsync(request.Token);
                if (user == null)
                    return BadRequest(new { message = "Invalid or expired reset link" });

                await _userService.UpdatePasswordAsync(user, request.NewPassword);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { message = "Error resetting password" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            // TODO: Implement JWT token generation
            // For now, return a placeholder
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Email}"));
        }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
