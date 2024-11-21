using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project_site.Controllers
{
    public class OrdersController : Controller
    {
        [HttpGet]
        [Authorize(Roles = "Sitter")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Client")]
        public IActionResult Create()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Sitter")]
        public IActionResult New()
        {
            return View();
        }
    }
}
