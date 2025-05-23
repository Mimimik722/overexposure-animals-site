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

namespace Project_site.Controllers
{
    public class UserController : Controller
    {
        readonly ApplicationContext db = ApplicationContext.GetInstance();

        //Получение хэша для пароля
        private static string GetHash(string input)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
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
            byte[] bytes = db.Users.FirstOrDefault(o => o.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Image;
            return File(bytes, "image/jpg");
        }

        public IActionResult Index()
        {
            try
            {
                ViewData["role"] = User.FindFirstValue(ClaimTypes.Role);
                if (db.Sitters.FirstOrDefault(o => o.User_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)) != null)
                {
                    SitterModel sitter = db.Sitters.FirstOrDefault(o => o.User_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
                    ViewData["status"] = sitter.Status;
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
                if (!db.Users.Any(o => o.Telephone == Request.Form["telephone"].ToString()) ||
                    !db.Users.Any(o => o.Password == old_password
                                || o.Password == new_password)
                    )
                {
                    ModelState.AddModelError("Telephone", "Неверный телефон или пароль");
                    return View(data);
                }

                UserModel user = db.Users.Where(o => o.Telephone == Request.Form["telephone"].ToString()).Include(o => o.Town_).Include(o => o.Role_).First();
                if (user.Password != new_password)
                {
                    user.Password = new_password;
                    db.Update(user);
                    db.SaveChanges();
                }
                HttpContext.Session.Set("user", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user)));

                var claims = new[] {
                    new Claim(ClaimTypes.MobilePhone, Request.Form["telephone"].ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.Surname, user.Surname),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("Town", user.Town_.Name),
                    new Claim(ClaimTypes.Gender, user.Sex),
                    new Claim(ClaimTypes.DateOfBirth, user.Birthday.ToString())
                };
                
                if (user.Email != null)
                {
                    Claim claim = new(ClaimTypes.Email, user.Email);
                    _ = claims.Append(claim);
                }

                if (user.Role_.Name == "sitter")
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Sitter");
                }
                else if (user.Role_.Name == "admin")
                {
                    claims[2] = new Claim(ClaimTypes.Role, "Admin");
                }

                if (user.Image == null)
                {
                    FileStream fileStream = System.IO.File.Open("wwwroot/images/standard-profile-image.jpg", FileMode.Open);
                    IFormFile image = new FormFile(fileStream, 0, fileStream.Length, "base-image", "base-image");
                    user.Image = ImageToByteString(image);
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
                    ModelState.AddModelError("Name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                    return View(user_data);
                }

                if (!db.Towns.Any(o => o.Name == Request.Form["town"].ToString()))
                {
                    ModelState.AddModelError("Town_", "Такого города не существует");
                    return View(user_data);
                }

                if (db.Users.Any(o => o.Telephone == Request.Form["telephone"].ToString()))
                {
                    ModelState.AddModelError("Telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                    return View(user_data);
                }

                if (Request.Form["password"] != Request.Form["password_repeat"])
                {
                    ModelState.AddModelError("Password", "Пароли не совпадают");
                    return View(user_data);
                }

                UserModel user = new(){
                    Id = db.Users.Count() + 1,
                    Town_ = db.Towns.Where(o => o.Name == Request.Form["town"].ToString()).First(),
                    Name = Request.Form["name"],
                    Surname = Request.Form["surname"],
                    Password = GetHash(Request.Form["password"].ToString()),
                    Email = Request.Form["email"],
                    Telephone = Request.Form["telephone"],
                    Role_ = db.Roles.FirstOrDefault(o => o.Name == "client"),
                    Birthday = DateOnly.Parse(Request.Form["birthday"].ToString())
                };

                if (Request.Form["radioM"] == "on")
                {
                    user.Sex = "м";
                }
                else
                {
                    user.Sex = "ж";
                }
                if (Request.Form.Files["Image"] != null)
                {
                    user.Image = ImageToByteString(Request.Form.Files["Image"]);
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
                user = db.Users.FirstOrDefault(x => x.Password == key);
                user ??= new(){ Password = "0" };
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
                        user = db.Users.FirstOrDefault(o => o.Password == user.Password);
                        user.Password = GetHash(Request.Form["password"]);
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
                ModelState.AddModelError("Password", "Пароли не совпадают");
                return View(user);
            }
            else
            {
                string? password = db.Users.Where(o => o.Email == user.Email).First().Password;
                MailMessage message = new("vov.efimov2015@yandex.ru", user.Email){
                    Subject = "Няня Гуляня: Восстановление пароля",
                    Body = "Ссылка для восстановления пароля: localhost:5050/User/Password_reset?id=" + password + "\nДанное сообщение отправлено автоматически, посьба на него не отвечать."
                };

                SmtpClient smtpClient = new("smtp.yandex.ru", 465){
                    EnableSsl = true,
                    Timeout = 10000,
                    Credentials = new NetworkCredential("vov.efimov2015@yandex.ru", "yzieyshcplrghusx", "smtp.yandex.ru")
                };
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
                if (db.Sitters.FirstOrDefault(o => o.User_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)) != null)
                {
                    ViewData["status"] = db.Sitters.FirstOrDefault(o => o.User_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Status;
                }
                UserModel user = db.Users.FirstOrDefault(o => o.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
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
            user = db.Users.Include(o => o.Town_).FirstOrDefault(o => o.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone));

            if (!new_user.Name.ToString().All(char.IsLetter) ||
                !new_user.Surname.ToString().All(char.IsLetter))
            {
                ModelState.AddModelError("Name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                return View(new_user);
            }
            if (!db.Towns.Any(o => o.Name == Request.Form["town"].ToString()))
            {
                ModelState.AddModelError("Town_", "Такого города не существует");
                return View(new_user);
            }
            if (db.Users.Any(o => o.Telephone == new_user.Telephone.ToString()) && new_user.Telephone != user.Telephone)
            {
                ModelState.AddModelError("Telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                return View(new_user);
            }

            SitterModel? sitter = db.Sitters.FirstOrDefault(o => o.User_.Telephone == user.Telephone);

            if (Request.Form["radioM"] == "on")
            {
                user.Sex = "м";
            }
            else
            {
                user.Sex = "ж";
            }
            user.Name = new_user.Name;
            user.Surname = new_user.Surname;
            user.Town_ = db.Towns.FirstOrDefault(o => o.Name == Request.Form["town"].ToString());
            user.Sex = new_user.Sex;
            user.Birthday = new_user.Birthday;
            user.Telephone = new_user.Telephone;
            user.Email = new_user.Email;
            if (sitter != null)
            {
                sitter.Status = Request.Form["status"];
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
                UserModel user = db.Users.Where(o => o.Telephone == HttpContext.User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.Town_).First();
                
                if (db.Sitters.Any(o => o.User_ == user))
                {
                    ModelState.AddModelError("Payment", "Заявка уже подана");
                    return View();
                }
                if (payment <= 0)
                {
                    ModelState.AddModelError("Payment", "Плата за заказа не может быть равна 0 или меньше");
                    return View();
                }
                if (experience < 0)
                {
                    ModelState.AddModelError("Experience", "Опыт работы не может быть меньше 0");
                    return View();
                }
                if (int.Parse(Request.Form["age_from"]) < 0 || int.Parse(Request.Form["age_to"]) < 0
                    || float.Parse(Request.Form["weight_from"]) < 0 || float.Parse(Request.Form["weight_to"]) < 0)
                {
                    ModelState.AddModelError("Is_verificated", "Параметры требований не должны быть отрицательными");
                    return View();
                }
                if (int.Parse(Request.Form["age_from"]) > int.Parse(Request.Form["age_to"]) || float.Parse(Request.Form["weight_from"]) > float.Parse(Request.Form["weight_to"]))
                {
                    ModelState.AddModelError("Is_verificated", "Начальное значение для ограничения не может быть больше конченого значения");
                    return View();
                }

                SitterModel sitter = new(){
                    Id = db.Sitters.Count() + 1,
                    User_ = user,
                    Payment = payment,
                    Experience = experience
                };
                db.Sitters.Add(sitter);
                Requirement requirement = new(){ Sitter_ = sitter, 
                    Age_from = int.Parse(Request.Form["age_from"]),
                    Age_to = int.Parse(Request.Form["age_to"]), 
                    Weight_from = float.Parse(Request.Form["weight_from"]),
                    Weight_to = float.Parse(Request.Form["weight_to"])
                };
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
