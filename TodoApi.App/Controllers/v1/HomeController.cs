using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TodoApi.App.Controllers.v1
{
    public class HomeController : Controller
    {

        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}