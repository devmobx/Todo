using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Devmobx.Todo.Core.Services;
using Devmobx.Todo.App.Services;
using Devmobx.Todo.Storage.Models.v1.Auth;
using Devmobx.Todo.Storage.Models.v1;
using Devmobx.Todo.Core.Attributes;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Devmobx.Todo.App.Controllers.v1
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
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existingUser = await _userService.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "Email already registered" });

                var user = await _userService.CreateUserAsync(
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    request.Password
                );

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
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                if (!UserService.VerifyPassword(request.Password, user.PasswordHash))
                    return Unauthorized(new { message = "Invalid email or password" });

                // Generate JWT token
                var token = GenerateJwtToken(user);

                Console.WriteLine($"✅ Token generated: {token.Substring(0, 20)}...");

                // Set authentication cookie
                var claimsIdentity = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
        }, "Cookies");

                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync("Cookies", claimsPrincipal, new AuthenticationProperties
                {
                    IsPersistent = request.RememberMe,
                    ExpiresUtc = request.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(7)
                        : DateTimeOffset.UtcNow.AddHours(1)
                });

                _logger.LogInformation("User logged in: {UserId}", user.Id);

                // ✅ IMPORTANT: Return token in response
                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,  // ← This must be included
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

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("Cookies");
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Logout failed" });
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

                var resetLink = $"{Request.Scheme}://{Request.Host}/forgot-password?token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                _logger.LogInformation("Password reset requested for user: {UserId}", user.Id);

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

                _logger.LogInformation("Password reset for user: {UserId}", user.Id);

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
            var secretKey = "TodoApp-SuperSecretKey-MustBe32CharactersLongForHS256!!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, user.FirstName),
        new Claim(ClaimTypes.Surname, user.LastName),
    };

            var token = new JwtSecurityToken(
                issuer: "TodoApp",
                audience: "TodoAppUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
