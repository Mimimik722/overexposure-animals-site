using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_site.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace Project_site.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public ApplicationContext db;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            try
            {
                db = new();
            }
            catch
            {
                StatusCode(504);
            }
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("user") != null)
            {
                ApplicationContext db = new();
                ViewBag.pets = db.Pets.Where(o => o.client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.breed_);
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        public IActionResult Chat(int phone)
        {
            if (User.FindFirstValue(ClaimTypes.MobilePhone) != phone.ToString()) 
            {
            UserChatModel userChat = new UserChatModel();

            int senderId = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).id;
            string? name = User.Identity.Name;

            userChat.LoggedInUser = new UserModel { id = senderId, name = name };

            userChat.Receiver = db.Users.FirstOrDefault(o => o.telephone == phone.ToString());
            return View(userChat);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        public ActionResult GetChatConversion(int receiverId)
        {
            UserModel user;
            user = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
            int loginUserId = user.id;
            var chatHistories = db.UserChatHistory.Include("sender_")
                                .Include("receiver_").Where(a => (a.receiver_ == user && a.sender_.id == receiverId)
                               || (a.receiver_.id == receiverId && a.sender_ == user)).OrderByDescending(a => a.created_at).ToList();
            ViewData["loginUserId"] = loginUserId;
            return PartialView("_ChatConversion", chatHistories);
        }
    }
}
