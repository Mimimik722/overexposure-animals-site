using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_site.Models;

namespace Project_site.Controllers
{
    
    public class SittersController : Controller
    {
        ApplicationContext db = new();

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            try
            {
                IEnumerable<SitterModel> sitters = db.Sitters.Where(o => o.is_verificated == 1).Include(o => o.user_).Include(o => o.user_.town_).ToList();
                return View(sitters);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Applications()
        {
            try
            {
                IEnumerable<SitterModel> sitters = db.Sitters.Where(o => o.is_verificated == 0).Include(o => o.user_).Include(o => o.user_.town_).ToList();
                return View(sitters);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        [Authorize(Roles = "User, Admin")]
        public IActionResult Profile(int sitter_id)
        {
            try
            {
                SitterModel sitter = db.Sitters.Include(o => o.user_).Include(o => o.user_.town_).SingleOrDefault(o => o.id == sitter_id);
                ViewData["rating"] = (float)db.Orders.Where(o => o.Sitter_ == sitter && o.Feedback_ != null).Average(o => o.Feedback_.Rating);
                return View(sitter);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Approve(int sitter_id)
        {
            try
            {
                SitterModel sitter = db.Sitters.SingleOrDefault(o => o.id == sitter_id);
                sitter.is_verificated = 1;
                db.Sitters.Update(sitter);
                db.SaveChanges();
                return RedirectToAction("Applications");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Админинстратор сможет сам добавлять ситтеров из числа зарегистрированных пользователей
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Remove(int sitter_id)
        {
            try
            {
                SitterModel sitter = db.Sitters.Include(o => o.user_).SingleOrDefault(o => o.id == sitter_id);
                db.Sitters.Remove(sitter);
                db.SaveChanges();
                return RedirectToAction("Applications");
            }
            catch
            {
                return StatusCode(504);
            }
        }
    }
}
