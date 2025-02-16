using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_site.Models;

namespace Project_site.Controllers
{

    public class SittersController : Controller
    {
        ApplicationContext db;
        public SittersController()
        {
            try
            {
                db = new();
            }
            catch
            {
                StatusCode(504);
            }
        }

        //Просмотр списка ситтеров
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            try
            {
                ICollection<SitterModel> sitters = db.Sitters.Where(o => o.is_verificated == 1).Include(o => o.user_).Include(o => o.user_.town_).ToList();
                return View(sitters);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Просмотр списка заявок на ситтера
        [Authorize(Roles = "Admin")]
        public IActionResult Applications()
        {
            try
            {
                ICollection<SitterModel> sitters = db.Sitters.Where(o => o.is_verificated == 0).Include(o => o.user_).Include(o => o.user_.town_).ToList();
                return View(sitters);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Просмотр инофмации о ситтере
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

        //Подтверждение заявки
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(int sitter_id)
        {
            try
            {
                SitterModel sitter = db.Sitters.Where(o => o.id == sitter_id).Include(o => o.user_.role_).First();
                sitter.is_verificated = 1;
                sitter.user_.role_ = db.Roles.FirstOrDefault(o => o.name == "sitter");
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
        //WIP
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(int? user_id)
        {
            if (user_id == null)
            {
                ICollection<UserModel> users = db.Users.Where(o => o.role_.name == "client").Include(o => o.town_).ToList();
                return View(users);
            }
            ViewData["Id"] = user_id;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(int user_id)
        {
            ViewData["Id"] = user_id;
            if (Request.Form["age_from"] == "" || Request.Form["age_to"] == "" ||
                Request.Form["weight_from"] == "" || Request.Form["weight_to"] == "" ||
                Request.Form["experience"] == "" || Request.Form["payment"] == "")
            {
                return View();
            }
            try
            {

                if (int.Parse(Request.Form["payment"]) <= 0 || int.Parse(Request.Form["experience"]) < 0 ||
                    int.Parse(Request.Form["age_from"]) < 0 || int.Parse(Request.Form["age_to"]) < 0
                    || float.Parse(Request.Form["weight_from"]) < 0 || float.Parse(Request.Form["weight_to"]) < 0 ||
                    int.Parse(Request.Form["age_from"]) > int.Parse(Request.Form["age_to"]) ||
                    float.Parse(Request.Form["weight_from"]) > float.Parse(Request.Form["weight_to"])
                    )
                {
                    return View(user_id);
                }

                UserModel user = db.Users.FirstOrDefault(o => o.id == user_id);
                user.role_ = db.Roles.FirstOrDefault(o => o.name == "sitter");

                SitterModel sitter = new SitterModel {
                    id = db.Sitters.Count() + 1,
                    user_ = user,
                    payment = int.Parse(Request.Form["payment"]),
                    experience = int.Parse(Request.Form["experience"]),
                    status = "Занят",
                    is_verificated = 1
                };
                db.Sitters.Add(sitter);
                
                Requirement requirement = new Requirement
                {
                    Sitter_ = sitter,
                    age_from = int.Parse(Request.Form["age_from"]),
                    age_to = int.Parse(Request.Form["age_to"]),
                    weight_from = float.Parse(Request.Form["weight_from"]),
                    weight_to = float.Parse(Request.Form["weight_to"])
                };
                db.Requirements.Add(requirement);
                
                db.SaveChanges();
                return RedirectToAction("Add");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Удаление пользователя из списка ситтеров
        [Authorize(Roles = "Admin")]
        public IActionResult Remove(int sitter_id)
        {
            try
            {
                SitterModel sitter = db.Sitters.Include(o => o.user_).SingleOrDefault(o => o.id == sitter_id);
                Requirement requirement = db.Requirements.FirstOrDefault(o => o.Sitter_ == sitter);

                db.Requirements.Remove(requirement);
                db.Sitters.Remove(sitter);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return StatusCode(504);
            }
        }
    }
}
