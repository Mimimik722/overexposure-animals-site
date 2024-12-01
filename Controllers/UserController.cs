using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Project_site.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Project_site.Controllers
{
    public class UserController : Controller
    {
        private string GetHash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }

        // GET: UserController/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: UserController/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel data)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                
                if (!db.Users.Any(o => o.telephone == Request.Form["telephone"].ToString()) ||
                    !db.Users.Any(o => o.password == GetHash(Request.Form["password"].ToString()).ToString()))
                {
                    ModelState.AddModelError("telephone", "Неверный телефон или пароль");
                    return View(data);
                }

                UserModel client = db.Users.Where(o => o.telephone == Request.Form["telephone"].ToString()).Include(o => o.town_).First();
                HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(client)));

                var claims = new[] {
                        new Claim(ClaimTypes.MobilePhone, Request.Form["telephone"].ToString()),
                        new Claim(ClaimTypes.Name, client.name),
                        new Claim(ClaimTypes.Role, "User")
                };
                if (db.Sitters.Any(o => o.user_ == client))
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Sitter");
                }
                else if (db.Admins.Any(o => o.user_ == client))
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Admin");
                }

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(client)));
                this.HttpContext.SignInAsync(claimsPrincipal);
                this.HttpContext.Session.CommitAsync();
                return Redirect("/");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        // GET: UserController/Registration
        [HttpGet]
        public IActionResult Registration()
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                ViewData["towns"] = db.Towns.ToList();
                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: UserController/Registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registration(UserModel clientM)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                ViewData["towns"] = db.Towns.ToList();

                if (!Request.Form["name"].ToString().All(char.IsLetter) ||
                    !Request.Form["surname"].ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                    return View(clientM);
                }

                if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
                {
                    ModelState.AddModelError("town_", "Такого города не существует");
                    return View(clientM);
                }

                if (db.Users.Any(o => o.telephone == Request.Form["telephone"].ToString()))
                {
                    ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                    return View(clientM);
                }

                if (Request.Form["password"] != Request.Form["password_repeat"])
                {
                    ModelState.AddModelError("password", "Пароли не совпадают");
                    return View(clientM);
                }

                UserModel client = new UserModel();

                client.id = db.Users.Count() + 1;
                client.town_ = db.Towns.Where(o => o.name == Request.Form["town"].ToString()).First();
                client.name = Request.Form["name"];
                client.surname = Request.Form["surname"];
                client.password = GetHash(Request.Form["password"].ToString());
                client.email = Request.Form["email"];

                if (Request.Form["radioM"] == "on")
                {
                    client.sex = "м";
                }
                else
                {
                    client.sex = "ж";
                }

                client.telephone = Request.Form["telephone"];
                client.birthday = DateOnly.Parse(Request.Form["birthday"].ToString());
                db.Users.Add(client);
                db.SaveChanges();

                var claims = new[] { new Claim("client", Request.Form["telephone"].ToString()) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(client)));
                this.HttpContext.SignInAsync(claimsPrincipal);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
            return Redirect("/");
        }

        public IActionResult Logout()
        {
            this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            this.HttpContext.Session.Remove("user");
            
            return Redirect("/");
        }

        // GET: UserController/Profile
        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            try
            {
                ApplicationContext db = new();
                UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
                ViewData["role"] = HttpContext.User.FindFirst(ClaimTypes.Role).Value;
                if (db.Sitters.FirstOrDefault(o => o.user_ == user) != null)
                {
                    ViewData["status"] = db.Sitters.FirstOrDefault(o => o.user_ == user);
                }
                return View(user);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        // GET: UserController/Edit
        [HttpGet]
        [Authorize]
        public IActionResult Edit()
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
                ViewData["towns"] = db.Towns.ToList();
                if (db.Sitters.FirstOrDefault(o => o.user_ == user) != null)
                {
                    ViewData["status"] = db.Sitters.FirstOrDefault(o => o.user_ == user);
                }
                return View(user);
            }
            catch
            {
                return StatusCode(503);
            }
        }

        // POST: UserController/Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserModel? new_user)
        {
            ApplicationContext db = new ApplicationContext();
            UserModel user = db.Users.Include(o => o.town_).FirstOrDefault(o => o == JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user")));
            SitterModel sitter = db.Sitters.FirstOrDefault(o => o.user_ == user);
            ViewData["towns"] = db.Towns.ToList();

            if (!new_user.name.ToString().All(char.IsLetter) ||
                !new_user.surname.ToString().All(char.IsLetter))
            {
                ModelState.AddModelError("name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                return View(new_user);
            }
            if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
            {
                ModelState.AddModelError("town_", "Такого города не существует");
                new_user.town_ = new Town { name = Request.Form["town"].ToString()};
                return View(new_user);
            }
            if (db.Users.Any(o => o.telephone == new_user.telephone.ToString()) && new_user.telephone != user.telephone)
            {
                ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
            }

            if (Request.Form["radioM"] == "on")
            {
                new_user.sex = "м";
            }
            else
            {
                new_user.sex = "ж";
            }

            if (user.name != new_user.name)
            {
                user.name = new_user.name;
            }
            if (user.surname != new_user.surname)
            {
                user.surname = new_user.surname;
            }
            if (user.town_.name != Request.Form["town"])
            {
                user.town_ = db.Towns.FirstOrDefault(o => o.name == Request.Form["town"].ToString());
            }
            if (user.sex != new_user.sex)
            {
                user.sex = new_user.sex;
            }
            if (user.birthday != new_user.birthday)
            {
                user.birthday = new_user.birthday;
            }
            if (user.telephone != new_user.telephone)
            {
                user.telephone = new_user.telephone;
            }
            if (user.email != new_user.email)
            {
                user.email = new_user.email;
            }
            if (sitter != null)
            {
                sitter.status = Request.Form["status"];
                db.Sitters.Update(sitter);
            }

            db.Users.Update(user);
            db.SaveChanges();
            this.HttpContext.Session.Remove("user");
            this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult Application()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult Application(int payment, int experience)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                UserModel user = db.Users.Where(o => o.telephone == HttpContext.User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.town_).First();
                
                if (db.Sitters.Any(o => o.user_ == user))
                {
                    ModelState.AddModelError("payment", "Заявка уже подана");
                    return View();
                }
                if (payment <= 0)
                {
                    ModelState.AddModelError("payment", "Плата за заказа не может быть равна 0 или меньше");
                    return View();
                }
                if (experience < 0)
                {
                    ModelState.AddModelError("experience", "Опыт работы не может быть меньше 0");
                    return View();
                }
                if (int.Parse(Request.Form["age_from"]) < 0 || int.Parse(Request.Form["age_to"]) < 0
                    || float.Parse(Request.Form["weight_from"]) < 0 || float.Parse(Request.Form["weight_to"]) < 0)
                {
                    ModelState.AddModelError("is_verificated", "Параметры требований не должны быть отрицательными");
                    return View();
                }
                if (int.Parse(Request.Form["age_from"]) > int.Parse(Request.Form["age_to"]) || float.Parse(Request.Form["weight_from"]) > float.Parse(Request.Form["weight_to"]))
                {
                    ModelState.AddModelError("is_verificated", "Начальное значение для ограничения не может быть больше конченого значения");
                    return View();
                }

                SitterModel sitter = new SitterModel {id = db.Sitters.Count() + 1,  user_ = user, payment = payment, experience = experience };
                db.Sitters.Add(sitter);
                Requirement requirement = new Requirement { Sitter_ = sitter, 
                    age_from = int.Parse(Request.Form["age_from"]), age_to = int.Parse(Request.Form["age_to"]), 
                    weight_from = float.Parse(Request.Form["weight_from"]), weight_to = float.Parse(Request.Form["weight_to"])};
                db.Requirements.Add(requirement);
                db.SaveChanges();
                
                return RedirectToAction("Index", "Home");
            }
            catch
            {
                return StatusCode(504);
            }
        }
    }
}
