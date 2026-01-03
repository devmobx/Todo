using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devmobx.Todo.App.Controllers.v1
{
    [Authorize]
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Login()
        {
            return RedirectToAction("Login", "AuthViews");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return RedirectToAction("Register", "AuthViews");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
