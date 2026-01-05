using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        private const string TokenEndpoint = "https://devmobx.ciamlogin.com/f4140220-24e0-40ca-a1cf-d00df7398019/oauth2/v2.0/token";

        public AuthController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

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
                return View(model);
            }

            var (tokenResponse, errorMessage) = await GetTokenAsync(model.Email, model.Password);

            if (tokenResponse == null)
            {
                _logger.LogError("Login failed: {Error}", errorMessage);
                ModelState.AddModelError("", errorMessage ?? "Invalid credentials");
                return View(model);
            }

            var claims = ParseIdToken(tokenResponse.IdToken);

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
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

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task<(TokenResponse?, string?)> GetTokenAsync(string username, string password)
        {
            var client = _httpClientFactory.CreateClient();

            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var scope = "openid profile email offline_access";

            _logger.LogInformation("Requesting token for user: {User}", username);
            _logger.LogInformation("Using ClientId: {ClientId}", clientId);
            _logger.LogInformation("Using Scope: {Scope}", scope);

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId!),
                new KeyValuePair<string, string>("client_secret", clientSecret!),
                new KeyValuePair<string, string>("scope", scope),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await client.PostAsync(TokenEndpoint, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Token response status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("Token response: {Response}", content);

            if (!response.IsSuccessStatusCode)
            {
                // Parse error response
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    var error = root.TryGetProperty("error", out var e) ? e.GetString() : "unknown";
                    var errorDescription = root.TryGetProperty("error_description", out var ed) ? ed.GetString() : content;

                    _logger.LogError("Token error: {Error} - {Description}", error, errorDescription);

                    return (null, errorDescription);
                }
                catch
                {
                    return (null, $"HTTP {response.StatusCode}: {content}");
                }
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);
            return (tokenResponse, null);
        }

        private List<Claim> ParseIdToken(string idToken)
        {
            var claims = new List<Claim>();

            if (string.IsNullOrEmpty(idToken))
            {
                _logger.LogWarning("ID token is null or empty");
                return claims;
            }

            var parts = idToken.Split('.');
            if (parts.Length != 3) return claims;

            try
            {
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

                _logger.LogInformation("ID token payload: {Payload}", json);

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

            var result = await CreateUserAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Registration failed");
                return View(model);
            }

            // Optionally auto-login after registration
            TempData["SuccessMessage"] = "Account created successfully. Please sign in.";
            return RedirectToAction("Login");
        }

        private async Task<(bool Success, string? ErrorMessage)> CreateUserAsync(RegisterViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];

            // First, get an access token for Microsoft Graph API
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", clientId!),
        new KeyValuePair<string, string>("client_secret", clientSecret!),
        new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
    });

            var tokenResponse = await client.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                tokenRequest);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var error = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Graph token: {Error}", error);
                return (false, "Unable to connect to identity service");
            }

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenContent);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

            // Create the user via Microsoft Graph API
            var userPayload = new
            {
                accountEnabled = true,
                displayName = model.DisplayName,
                mailNickname = model.Email.Split('@')[0],
                userPrincipalName = $"{model.Email.Replace("@", "_")}@devmobx.onmicrosoft.com",
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = false,
                    password = model.Password
                },
                identities = new[]
                {
            new
            {
                signInType = "emailAddress",
                issuer = "devmobx.onmicrosoft.com",
                issuerAssignedId = model.Email
            }
        }
            };

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(userPayload),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var createResponse = await client.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("User creation response: {StatusCode} - {Content}",
                createResponse.StatusCode, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                using var errorDoc = JsonDocument.Parse(createContent);
                var errorMessage = errorDoc.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();
                return (false, errorMessage);
            }

            return (true, null);
        }
    }
}
