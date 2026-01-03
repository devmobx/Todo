using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devmobx.Todo.App.Controllers.v1
{
    [AllowAnonymous]
    public class AuthViewsController : Controller
    {
        private readonly ILogger<AuthViewsController> _logger;

        public AuthViewsController(ILogger<AuthViewsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GET: /login
        /// Returns the login page
        /// </summary>
        [HttpGet("/login")]
        public IActionResult Login()
        {
            // Redirect to home if already authenticated
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Login/Login.cshtml");
        }

        /// <summary>
        /// GET: /register
        /// Returns the registration page
        /// </summary>
        [HttpGet("/register")]
        public IActionResult Register()
        {
            // Redirect to home if already authenticated
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Register/Register.cshtml");
        }

        /// <summary>
        /// GET: /forgot-password
        /// Returns the forgot password page
        /// </summary>
        [HttpGet("/forgot-password")]
        public IActionResult ForgotPassword()
        {
            // Redirect to home if already authenticated
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/ForgotPassword/ForgotPassword.cshtml");
        }
    }
}
