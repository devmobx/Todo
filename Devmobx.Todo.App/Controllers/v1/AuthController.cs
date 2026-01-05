using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Devmobx.Todo.App.Models.v1.Auth;

namespace Devmobx.Todo.App.Controllers.v1
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthController> _logger;

        private readonly string _tenantSubdomain;
        private readonly string _tenantId;
        private readonly string _clientId;

        // Base URL
        private string CiamBaseUrl => $"https://{_tenantSubdomain}.ciamlogin.com";

        // Sign-in endpoints
        private string SignInInitiateEndpoint => $"{CiamBaseUrl}/{_tenantId}/oauth2/v2.0/initiate";
        private string SignInChallengeEndpoint => $"{CiamBaseUrl}/{_tenantId}/oauth2/v2.0/challenge";
        private string TokenEndpoint => $"{CiamBaseUrl}/{_tenantId}/oauth2/v2.0/token";

        // Sign-up endpoints
        private string SignUpStartEndpoint => $"{CiamBaseUrl}/{_tenantId}/signup/v1.0/start";
        private string SignUpChallengeEndpoint => $"{CiamBaseUrl}/{_tenantId}/signup/v1.0/challenge";
        private string SignUpContinueEndpoint => $"{CiamBaseUrl}/{_tenantId}/signup/v1.0/continue";

        public AuthController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _tenantSubdomain = "devmobx";
            _tenantId = _configuration["AzureAd:TenantId"]!;
            _clientId = _configuration["AzureAd:ClientId"]!;
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            try
            {
                // Step 1: Initiate sign-in
                var initiateResult = await SignInInitiateAsync(model.Email);
                if (!initiateResult.Success)
                {
                    ModelState.AddModelError("", initiateResult.ErrorMessage ?? "Unable to start authentication");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }

                // Step 2: Submit password challenge
                var challengeResult = await SignInChallengeAsync(initiateResult.ContinuationToken!, model.Password);
                if (!challengeResult.Success)
                {
                    ModelState.AddModelError("", challengeResult.ErrorMessage ?? "Invalid credentials");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }

                // Step 3: Get tokens
                var tokenResult = await GetTokensAsync(challengeResult.ContinuationToken!);
                if (tokenResult.TokenResponse == null)
                {
                    ModelState.AddModelError("", tokenResult.ErrorMessage ?? "Failed to obtain tokens");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }

                // Sign in user
                await SignInUserAsync(tokenResult.TokenResponse, model.RememberMe);

                _logger.LogInformation("User {Email} logged in successfully", model.Email);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
        }

        // ==================== SIGN-IN FLOW ====================

        private async Task<(bool Success, string? ContinuationToken, string? ErrorMessage)> SignInInitiateAsync(string email)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("challenge_type", "password oob redirect"),
                new KeyValuePair<string, string>("username", email)
            });

            _logger.LogInformation("Calling SignIn Initiate: {Endpoint}", SignInInitiateEndpoint);

            var response = await client.PostAsync(SignInInitiateEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignIn Initiate: {StatusCode} - {Content}", response.StatusCode, content);

            return ParseContinuationResponse(content);
        }

        private async Task<(bool Success, string? ContinuationToken, string? ErrorMessage)> SignInChallengeAsync(
            string continuationToken, string password)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("continuation_token", continuationToken),
                new KeyValuePair<string, string>("challenge_type", "password"),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await client.PostAsync(SignInChallengeEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignIn Challenge: {StatusCode} - {Content}", response.StatusCode, content);

            return ParseContinuationResponse(content, friendlyErrors: true);
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ==================== REGISTRATION (STEP 1: EMAIL) ====================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Step 1: Start signup
                var startResult = await SignUpStartAsync(model.Email);
                if (!startResult.Success)
                {
                    ModelState.AddModelError("", startResult.ErrorMessage ?? "Unable to start registration");
                    return View(model);
                }

                // Step 2: Request OTP challenge (sends email to user)
                var challengeResult = await SignUpChallengeAsync(startResult.ContinuationToken!);
                if (!challengeResult.Success)
                {
                    ModelState.AddModelError("", challengeResult.ErrorMessage ?? "Unable to send verification code");
                    return View(model);
                }

                // Store data in TempData for next step
                TempData["SignUpEmail"] = model.Email;
                TempData["SignUpPassword"] = model.Password;
                TempData["SignUpContinuationToken"] = challengeResult.ContinuationToken;

                // Redirect to OTP verification page
                return RedirectToAction("VerifyEmail");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        // ==================== REGISTRATION (STEP 2: VERIFY EMAIL) ====================

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            var email = TempData.Peek("SignUpEmail") as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;
            return View(new VerifyEmailViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            var email = TempData.Peek("SignUpEmail") as string;
            var password = TempData.Peek("SignUpPassword") as string;
            var continuationToken = TempData.Peek("SignUpContinuationToken") as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(continuationToken))
            {
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Step 3: Submit OTP to verify email
                var otpResult = await SignUpSubmitOtpAsync(continuationToken, model.Code);
                if (!otpResult.Success)
                {
                    ModelState.AddModelError("", otpResult.ErrorMessage ?? "Invalid verification code");
                    return View(model);
                }

                // Step 4: Submit password
                var passwordResult = await SignUpSubmitPasswordAsync(otpResult.ContinuationToken!, password);
                if (!passwordResult.Success)
                {
                    ModelState.AddModelError("", passwordResult.ErrorMessage ?? "Registration failed");
                    return View(model);
                }

                // Clear TempData
                TempData.Remove("SignUpEmail");
                TempData.Remove("SignUpPassword");
                TempData.Remove("SignUpContinuationToken");

                TempData["SuccessMessage"] = "Account created successfully! Please sign in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification error for {Email}", email);
                ModelState.AddModelError("", "An error occurred. Please try again.");
                return View(model);
            }
        }

        // ==================== SIGN-UP FLOW ====================

        private async Task<(bool Success, string? ContinuationToken, string? ErrorMessage)> SignUpStartAsync(string email)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("challenge_type", "oob password redirect"),
                new KeyValuePair<string, string>("username", email)
            });

            _logger.LogInformation("Calling SignUp Start: {Endpoint}", SignUpStartEndpoint);

            var response = await client.PostAsync(SignUpStartEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignUp Start: {StatusCode} - {Content}", response.StatusCode, content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, $"Server returned empty response. Status: {response.StatusCode}");
            }

            if (!content.TrimStart().StartsWith("{"))
            {
                _logger.LogError("Non-JSON response: {Content}", content);
                return (false, null, "Invalid response from authentication server");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                var errorCode = error.GetString();
                var errorDesc = root.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : errorCode;

                var friendlyMessage = errorCode switch
                {
                    "user_already_exists" => "An account with this email already exists. Please sign in instead.",
                    "invalid_request" => "Invalid email address format",
                    _ => errorDesc
                };

                return (false, null, friendlyMessage);
            }

            if (root.TryGetProperty("continuation_token", out var token))
            {
                return (true, token.GetString(), null);
            }

            return (false, null, "Unexpected response from server");
        }

        private async Task<(bool Success, string? ContinuationToken, string? ErrorMessage)> SignUpChallengeAsync(
            string continuationToken)
        {
            var client = _httpClientFactory.CreateClient();

            // Request OTP challenge - this sends the verification email
            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("continuation_token", continuationToken),
                new KeyValuePair<string, string>("challenge_type", "oob")
            });

            _logger.LogInformation("Calling SignUp Challenge: {Endpoint}", SignUpChallengeEndpoint);

            var response = await client.PostAsync(SignUpChallengeEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignUp Challenge: {StatusCode} - {Content}", response.StatusCode, content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, $"Server returned empty response. Status: {response.StatusCode}");
            }

            return ParseContinuationResponse(content);
        }

        private async Task<(bool Success, string? ContinuationToken, string? ErrorMessage)> SignUpSubmitOtpAsync(
            string continuationToken, string otp)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("continuation_token", continuationToken),
                new KeyValuePair<string, string>("grant_type", "oob"),
                new KeyValuePair<string, string>("oob", otp)
            });

            _logger.LogInformation("Calling SignUp Continue (OTP): {Endpoint}", SignUpContinueEndpoint);

            var response = await client.PostAsync(SignUpContinueEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignUp OTP: {StatusCode} - {Content}", response.StatusCode, content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, $"Verification failed. Status: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check for continuation token FIRST - this indicates we can proceed
            // Even with "credential_required" error, if there's a continuation_token, OTP was verified 【0】
            if (root.TryGetProperty("continuation_token", out var nextToken))
            {
                var tokenValue = nextToken.GetString();

                // credential_required with continuation_token = OTP verified, now need password
                if (root.TryGetProperty("error", out var error))
                {
                    var errorCode = error.GetString();
                    if (errorCode == "credential_required")
                    {
                        _logger.LogInformation("OTP verified successfully, credential (password) required next");
                        return (true, tokenValue, null);
                    }
                }

                // Any continuation token means success
                return (true, tokenValue, null);
            }

            // Handle actual errors (no continuation token)
            if (root.TryGetProperty("error", out var errorProp))
            {
                var errorCode = errorProp.GetString();
                var errorDesc = root.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : errorCode;

                var friendlyMessage = errorCode switch
                {
                    "invalid_grant" => "Invalid or expired verification code",
                    "expired_token" => "Verification code has expired. Please start over.",
                    "invalid_oob_value" => "Invalid verification code. Please check and try again.",
                    _ => errorDesc
                };

                return (false, null, friendlyMessage);
            }

            // Success without continuation token (unlikely)
            if (response.IsSuccessStatusCode)
            {
                return (true, null, null);
            }

            return (false, null, "Unexpected response from server");
        }

        private async Task<(bool Success, string? ErrorMessage)> SignUpSubmitPasswordAsync(
            string continuationToken, string password)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("continuation_token", continuationToken),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("password", password)
            });

            _logger.LogInformation("Calling SignUp Continue (password): {Endpoint}", SignUpContinueEndpoint);

            var response = await client.PostAsync(SignUpContinueEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SignUp Password: {StatusCode} - {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(content))
            {
                return (true, null);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, $"Registration failed. Status: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                var errorCode = error.GetString();
                var errorDesc = root.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : errorCode;

                var friendlyMessage = errorCode switch
                {
                    "password_too_weak" => "Password must be at least 8 characters with uppercase, lowercase, numbers, and special characters",
                    "password_too_short" => "Password is too short (minimum 8 characters)",
                    "password_too_long" => "Password is too long",
                    "password_banned" => "This password is not allowed. Please choose another.",
                    _ => errorDesc
                };

                return (false, friendlyMessage);
            }

            return (true, null);
        }

        // ==================== TOKEN HANDLING ====================

        private async Task<(TokenResponse? TokenResponse, string? ErrorMessage)> GetTokensAsync(string continuationToken)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("continuation_token", continuationToken),
                new KeyValuePair<string, string>("grant_type", "continuation_token"),
                new KeyValuePair<string, string>("scope", "openid profile email offline_access")
            });

            var response = await client.PostAsync(TokenEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Token: {StatusCode} - {Content}", response.StatusCode, content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return (null, "Empty response from token endpoint");
            }

            if (!response.IsSuccessStatusCode)
            {
                using var errorDoc = JsonDocument.Parse(content);
                var errorDesc = errorDoc.RootElement.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : "Failed to obtain tokens";
                return (null, errorDesc);
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);
            return (tokenResponse, null);
        }

        private async Task SignInUserAsync(TokenResponse tokenResponse, bool rememberMe)
        {
            var claims = ParseIdToken(tokenResponse.IdToken);
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };

            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken ?? "" },
                new AuthenticationToken { Name = "id_token", Value = tokenResponse.IdToken }
            });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        // ==================== HELPERS ====================

        private (bool Success, string? ContinuationToken, string? ErrorMessage) ParseContinuationResponse(
            string content, bool friendlyErrors = false)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, "Empty response from server");
            }

            if (!content.TrimStart().StartsWith("{") && !content.TrimStart().StartsWith("["))
            {
                _logger.LogError("Non-JSON response: {Content}", content);
                return (false, null, "Invalid response from authentication server");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                var errorCode = error.GetString();
                var errorDesc = root.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : errorCode;

                string? friendlyMessage = errorDesc;
                if (friendlyErrors)
                {
                    friendlyMessage = errorCode switch
                    {
                        "invalid_grant" => "Invalid email or password",
                        "invalid_client" => "Authentication configuration error",
                        "user_not_found" => "No account found with this email",
                        "invalid_credentials" => "Invalid email or password",
                        _ => errorDesc
                    };
                }

                return (false, null, friendlyMessage);
            }

            if (root.TryGetProperty("continuation_token", out var token))
            {
                return (true, token.GetString(), null);
            }

            return (false, null, "Unexpected response from server");
        }

        private List<Claim> ParseIdToken(string idToken)
        {
            var claims = new List<Claim>();
            if (string.IsNullOrEmpty(idToken)) return claims;

            var parts = idToken.Split('.');
            if (parts.Length != 3) return claims;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

                _logger.LogInformation("ID token claims: {Claims}", json);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("sub", out var sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString()!));
                if (root.TryGetProperty("name", out var name))
                    claims.Add(new Claim(ClaimTypes.Name, name.GetString()!));
                if (root.TryGetProperty("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.GetString()!));
                if (root.TryGetProperty("oid", out var oid))
                    claims.Add(new Claim("oid", oid.GetString()!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ID token");
            }

            return claims;
        }
    }
}
