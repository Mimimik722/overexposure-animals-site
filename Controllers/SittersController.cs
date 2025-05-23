using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_site.Models;

namespace Project_site.Controllers
{

    public class SittersController : Controller
    {
        readonly ApplicationContext db = ApplicationContext.GetInstance();

        //Просмотр списка ситтеров
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            try
            {
                ICollection<SitterModel> sitters = [.. db.Sitters.Where(o => o.Is_verificated == 1).Include(o => o.User_).Include(o => o.User_.Town_)];
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
                ICollection<SitterModel> sitters = [.. db.Sitters.Where(o => o.Is_verificated == 0).Include(o => o.User_).Include(o => o.User_.Town_)];
                return View(sitters);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Просмотр инофмации о ситтере
        [Authorize(Roles = "User, Admin")]
        public IActionResult Profile(int sitter)
        {
            try
            {
                SitterModel sitterM = db.Sitters.Include(o => o.User_).Include(o => o.User_.Town_).SingleOrDefault(o => o.Id == sitter);
                ViewData["rating"] = (float)db.Orders.Where(o => o.Sitter_ == sitterM && o.Feedback_ != null).Average(o => o.Feedback_.Rating);
                return View(sitterM);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Подтверждение заявки
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(int sitter)
        {
            try
            {
                SitterModel sitterM = db.Sitters.Where(o => o.Id == sitter).Include(o => o.User_.Role_).First();
                sitterM.Is_verificated = 1;
                sitterM.User_.Role_ = db.Roles.FirstOrDefault(o => o.Name == "sitter");
                db.Sitters.Update(sitterM);
                db.SaveChanges();
                return RedirectToAction("Applications");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Админинстратор сможет сам добавлять ситтеров из числа зарегистрированных пользователей
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(int? user)
        {
            if (user == null)
            {
                ICollection<UserModel> users = [.. db.Users.Where(o => o.Role_.Name == "client").Include(o => o.Town_)];
                return View(users);
            }
            ViewData["Id"] = user;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(int user)
        {
            ViewData["Id"] = user;
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
                    return View(user);
                }

                UserModel userM = db.Users.FirstOrDefault(o => o.Id == user);
                userM.Role_ = db.Roles.FirstOrDefault(o => o.Name == "sitter");

                SitterModel sitter = new(){
                    Id = db.Sitters.Count() + 1,
                    User_ = userM,
                    Payment = int.Parse(Request.Form["payment"]),
                    Experience = int.Parse(Request.Form["experience"]),
                    Status = "Занят",
                    Is_verificated = 1
                };
                db.Sitters.Add(sitter);
                
                Requirement requirement = new(){
                    Sitter_ = sitter,
                    Age_from = int.Parse(Request.Form["age_from"]),
                    Age_to = int.Parse(Request.Form["age_to"]),
                    Weight_from = float.Parse(Request.Form["weight_from"]),
                    Weight_to = float.Parse(Request.Form["weight_to"])
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
        public IActionResult Remove(int sitter)
        {
            try
            {
                SitterModel sitterM = db.Sitters.Include(o => o.User_).SingleOrDefault(o => o.Id == sitter);
                Requirement requirement = db.Requirements.FirstOrDefault(o => o.Sitter_ == sitterM);

                db.Requirements.Remove(requirement);
                db.Sitters.Remove(sitterM);
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
