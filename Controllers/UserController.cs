using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Project_site.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Project_site;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Project_site.Controllers
{
    public class UserController : Controller
    {
        ApplicationContext db;
        public UserController()
        {
            db = new();
        }
        //Получение хэша для пароля
        private string GetHash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }

        //Перевод изображения в байты
        public byte[] ImageToByteString(IFormFile image)
        {
            var memoryStream = new MemoryStream();
            image.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        [Authorize]
        public IActionResult ProfileImage()
        {
            byte[] bytes = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).image;
            return File(bytes, "image/jpg");
        }

        public IActionResult Index()
        {
            try
            {
                ViewData["role"] = User.FindFirstValue(ClaimTypes.Role);
                if (db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)) != null)
                {
                    SitterModel sitter = db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
                    ViewData["status"] = sitter.status;
                    ViewData["rating"] = (float)db.Orders.Where(o => o.Sitter_ == sitter && o.Feedback_ != null).Average(o => o.Feedback_.Rating);
                }
                return View(User);
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Открытие страницы с формой для входа
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //Верификация и валидация введённых данных с последующим входом
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel data)
        {
            Encryption enc = new();
            try
            {
                string old_password = GetHash(Request.Form["password"]),
                    new_password = enc.GetHash(Request.Form["password"].ToString(), double.Parse(Request.Form["telephone"]));
                if (!db.Users.Any(o => o.telephone == Request.Form["telephone"].ToString()) ||
                    !db.Users.Any(o => o.password == old_password
                                || o.password == new_password)
                    )
                {
                    ModelState.AddModelError("telephone", "Неверный телефон или пароль");
                    return View(data);
                }

                UserModel user = db.Users.Where(o => o.telephone == Request.Form["telephone"].ToString()).Include(o => o.town_).Include(o => o.role_).First();
                if (user.password != new_password)
                {
                    user.password = new_password;
                    db.Update(user);
                    db.SaveChanges();
                }
                HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));

                var claims = new[] {
                    new Claim(ClaimTypes.MobilePhone, Request.Form["telephone"].ToString()),
                    new Claim(ClaimTypes.Name, user.name),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.Surname, user.surname),
                    new Claim(ClaimTypes.Email, user.email),
                    new Claim("Town", user.town_.name),
                    new Claim(ClaimTypes.Gender, user.sex),
                    new Claim(ClaimTypes.DateOfBirth, user.birthday.ToString())
                };
                
                if (user.email != null)
                {
                    Claim claim = new Claim(ClaimTypes.Email, user.email);
                    claims.Append(claim);
                }

                if (user.role_.name == "sitter")
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Sitter");
                }
                else if (user.role_.name == "admin")
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Admin");
                }

                if (user.image == null)
                {
                    FileStream fileStream = System.IO.File.Open("wwwroot/images/standard-profile-image.jpg", FileMode.Open);
                    IFormFile image = new FormFile(fileStream, 0, fileStream.Length, "base-image", "base-image");
                    user.image = ImageToByteString(image);
                    db.Update(user);
                    db.SaveChanges();
                }

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));
                this.HttpContext.SignInAsync(claimsPrincipal);
                this.HttpContext.Session.CommitAsync();
                return Redirect("/");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Открытие окна регистрации
        [HttpGet]
        public IActionResult Registration()
        {
            try
            {
                ViewData["towns"] = db.Towns.ToList();
                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        //Обработка данных для регистрации
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registration(UserModel user_data)
        {
            try
            {
                ViewData["towns"] = db.Towns.ToList();

                if (!Request.Form["name"].ToString().All(char.IsLetter) ||
                    !Request.Form["surname"].ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                    return View(user_data);
                }

                if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
                {
                    ModelState.AddModelError("town_", "Такого города не существует");
                    return View(user_data);
                }

                if (db.Users.Any(o => o.telephone == Request.Form["telephone"].ToString()))
                {
                    ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                    return View(user_data);
                }

                if (Request.Form["password"] != Request.Form["password_repeat"])
                {
                    ModelState.AddModelError("password", "Пароли не совпадают");
                    return View(user_data);
                }

                UserModel user = new();
                user.id = db.Users.Count() + 1;
                user.town_ = db.Towns.Where(o => o.name == Request.Form["town"].ToString()).First();
                user.name = Request.Form["name"];
                user.surname = Request.Form["surname"];
                user.password = GetHash(Request.Form["password"].ToString());
                user.email = Request.Form["email"];
                user.telephone = Request.Form["telephone"];
                user.role_ = db.Roles.FirstOrDefault(o => o.name == "client");
                user.birthday = DateOnly.Parse(Request.Form["birthday"].ToString());

                if (Request.Form["radioM"] == "on")
                {
                    user.sex = "м";
                }
                else
                {
                    user.sex = "ж";
                }
                if (Request.Form.Files["Image"] != null)
                {
                    user.image = ImageToByteString(Request.Form.Files["Image"]);
                }

                db.Users.Add(user);
                db.SaveChanges();

                var claims = new[] { new Claim("client", Request.Form["telephone"].ToString()) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));
                this.HttpContext.SignInAsync(claimsPrincipal);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
            return Redirect("/");
        }

        //Восстановление пароля
        public IActionResult Password_reset(string? key){
            UserModel? user = new();
            if (!string.IsNullOrEmpty(key))
            {
                user = db.Users.FirstOrDefault(x => x.password == key);
                if (user == null)
                {
                    user = new();
                    user.password = "0";
                }
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult Password_reset(UserModel user)
        {
            if (Request.Form.ContainsKey("password"))
            {
                if (Request.Form["password"] == Request.Form["password_repeat"])
                {
                    try
                    {
                        user = db.Users.FirstOrDefault(o => o.password == user.password);
                        user.password = GetHash(Request.Form["password"]);
                        db.Users.Update(user);
                        db.SaveChanges();
                        return RedirectToAction("Login");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        return View(user);
                    }
                }
                ModelState.AddModelError("password", "Пароли не совпадают");
                return View(user);
            }
            else
            {
                string? password = db.Users.Where(o => o.email == user.email).First().password;
                MailMessage message = new("vov.efimov2015@yandex.ru", user.email); 
                message.Subject = "Няня Гуляня: Восстановление пароля";
                message.Body = "Ссылка для восстановления пароля: localhost:5043/User/Password_reset?id=" + password + "\nДанное сообщение отправлено автоматически, посьба на него не отвечать.";

                SmtpClient smtpClient = new("smtp.yandex.ru", 465);
                smtpClient.EnableSsl = true;
                smtpClient.Timeout = 10000;
                smtpClient.Credentials = new NetworkCredential("vov.efimov2015@yandex.ru", "yzieyshcplrghusx", "smtp.yandex.ru");
                try
                {
                    smtpClient.Send(message);
                    Console.WriteLine("Email Sent Successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                return View(user);
            }
        }

        //Выход из профиля
        public IActionResult Logout()
        {
            this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            this.HttpContext.Session.Remove("user");
            
            return Redirect("/");
        }

        //Переход на страницу с возможностью измнения информации о себе
        [HttpGet]
        [Authorize]
        public IActionResult Edit()
        {
            try
            {
                ViewData["towns"] = db.Towns.ToList();
                if (db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)) != null)
                {
                    ViewData["status"] = db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).status;
                }
                UserModel user = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
                return View(user);
            }
            catch
            {
                return StatusCode(503);
            }
        }

        //Изменение информации о себе
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserModel? new_user)
        {
            ViewData["towns"] = db.Towns.ToList();
            UserModel? user = new();
            user = db.Users.Include(o => o.town_).FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));

            if (!new_user.name.ToString().All(char.IsLetter) ||
                !new_user.surname.ToString().All(char.IsLetter))
            {
                ModelState.AddModelError("name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                return View(new_user);
            }
            if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
            {
                ModelState.AddModelError("town_", "Такого города не существует");
                return View(new_user);
            }
            if (db.Users.Any(o => o.telephone == new_user.telephone.ToString()) && new_user.telephone != user.telephone)
            {
                ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                return View(new_user);
            }

            SitterModel? sitter = db.Sitters.FirstOrDefault(o => o.user_.telephone == user.telephone);

            if (Request.Form["radioM"] == "on")
            {
                user.sex = "м";
            }
            else
            {
                user.sex = "ж";
            }
            user.name = new_user.name;
            user.surname = new_user.surname;
            user.town_ = db.Towns.FirstOrDefault(o => o.name == Request.Form["town"].ToString());
            user.sex = new_user.sex;
            user.birthday = new_user.birthday;
            user.telephone = new_user.telephone;
            user.email = new_user.email;
            if (sitter != null)
            {
                sitter.status = Request.Form["status"];
                db.Sitters.Update(sitter);
            }

            db.Users.Update(user);
            db.SaveChanges();
            this.HttpContext.Session.Remove("user");
            this.HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));
            return RedirectToAction("Index");
        }

        //Открытие формы для подачи заявки на ситтера
        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult Application()
        {
            return View();
        }

        //Подтверждение и отправка формы
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult Application(int payment, int experience)
        {
            try
            {
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
