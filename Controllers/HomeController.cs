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
        readonly ApplicationContext db = ApplicationContext.GetInstance();
        
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.pets = db.Pets.Where(o => o.Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.Breed_);
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
        public IActionResult Chat(long user)
        {
            if (User.FindFirstValue(ClaimTypes.MobilePhone) != user.ToString()) 
            {
                UserChatModel userChat = new();
                
                int sId = db.Users.FirstOrDefault(o => o.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Id;
                string? name = User.Identity.Name;

                userChat.LoggedInUser = new UserModel { Id = sId, Name = name };

                userChat.Receiver = db.Users.FirstOrDefault(o => o.Telephone == user.ToString());
                int rId = userChat.Receiver.Id;
                if (sId > rId)
                {
                    userChat.ChatId = ((sId + rId) * (sId + rId + 1) / 2 + rId).ToString();
                }
                else
                {
                    userChat.ChatId = ((rId + sId) * (rId + sId + 1) / 2 + sId).ToString();
                }
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
            user = db.Users.FirstOrDefault(o => o.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
            int loginUserId = user.Id;
            var chatHistories = db.UserChatHistory
                .Include(o => o.Sender_)
                .Include(o => o.Receiver_)
                .Where(a => (a.Receiver_ == user && a.Sender_.Id == receiverId)||
                    (a.Receiver_.Id == receiverId && a.Sender_ == user))
                .OrderBy(a => a.Created_at)
                .ToList();
            ViewData["loginUserId"] = loginUserId;
            return PartialView("_ChatConversion", chatHistories);
        }
    }
}
