using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Project_site.Models;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Session;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Project_site.Controllers
{
    public class ClientController : Controller
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
        public IActionResult Login(ClientModel data)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                if (!db.Clients.Any(o => o.telephone == Request.Form["telephone"].ToString()) ||
                    !db.Clients.Any(o => o.password == GetHash(Request.Form["password"].ToString()).ToString()))
                {
                    ModelState.AddModelError("telephone", "Неверный телефон или пароль");
                    return View(data);
                }
                Client client = db.Clients.Where(o => o.telephone == Request.Form["telephone"].ToString()).First();
                this.HttpContext.Session.Set("client", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(client)));
                var claims = new[] { new Claim("client", Request.Form["telephone"].ToString()) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.SignInAsync(claimsPrincipal);
                return Redirect("/");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
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
        public IActionResult Registration(ClientModel clientM)
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

                if (!db.Towns.Any(o => o.name == clientM.town.ToString()))
                {
                    ModelState.AddModelError("town", "Такого города не существует");
                    return View(clientM);
                }

                if (db.Clients.Any(o => o.telephone == Request.Form["telephone"].ToString()))
                {
                    ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
                }

                if (Request.Form["password"] != Request.Form["password_repeat"])
                {
                    ModelState.AddModelError("password_repeat", "Пароли не совпадают");
                    return View(clientM);
                }

                Client client = new Client();

                client.id = db.Clients.Count() + 1;
                client.town_id = db.Towns.Where(o => o.name == clientM.town.ToString()).First().id;
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
                db.Clients.Add(client);
                db.SaveChanges();

                var claims = new[] { new Claim("client", Request.Form["telephone"].ToString()) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                this.HttpContext.Session.Set("client", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(client)));
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
            this.HttpContext.Session.Remove("client");
            return Redirect("/");
        }

        // GET: UserController/Profile
        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        // GET: UserController/Edit
        [HttpGet]
        [Authorize]
        public IActionResult Edit()
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                ClientModel client = JsonConvert.DeserializeObject<ClientModel>(HttpContext.Session.GetString("client"));
                ViewData["towns"] = db.Towns.ToList();
                int? town = JsonConvert.DeserializeObject<Client>(HttpContext.Session.GetString("client")).town_id;
                client.town = db.Towns.Where(o => o.id == town).First().name.ToString();
                return View(client);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: UserController/Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ClientModel? new_client)
        {
            ApplicationContext db = new ApplicationContext();
            ClientModel client = JsonConvert.DeserializeObject<ClientModel>(HttpContext.Session.GetString("client"));
            Client db_client = db.Clients.Where(o => o.telephone == client.telephone.ToString()).First();
            int? town = JsonConvert.DeserializeObject<Client>(HttpContext.Session.GetString("client")).town_id;

            ViewData["towns"] = db.Towns.ToList();
            client.town = db.Towns.Where(o => o.id == town).First().name.ToString();

            if (!new_client.name.ToString().All(char.IsLetter) ||
                !new_client.surname.ToString().All(char.IsLetter))
            {
                ModelState.AddModelError("name", "Имя или Фамилия не должны содержать спец. символы или цифры");
                return View(new_client);
            }
            if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
            {
                ModelState.AddModelError("town", "Такого города не существует");
                return View(new_client);
            }
            if (db.Clients.Any(o => o.telephone == new_client.telephone.ToString()) && new_client.telephone != client.telephone)
            {
                ModelState.AddModelError("telephone", "Пользователь с таким номером телефона уже зарергистрирован");
            }

            if (Request.Form["radioM"] == "on")
            {
                new_client.sex = "м";
            }
            else
            {
                new_client.sex = "ж";
            }

            if (client.name != new_client.name)
            {
                client.name = new_client.name;
                db_client.name = new_client.name;
            }
            if (client.surname != new_client.surname)
            {
                client.surname = new_client.surname;
                db_client.surname = new_client.surname;
            }
            if (client.town != new_client.town)
            {
                client.town = new_client.town;
                db_client.town_id = db.Towns.Where(o => o.name == Request.Form["town"].ToString()).First().id;
            }
            if (client.sex != new_client.sex)
            {
                client.sex = new_client.sex;
                db_client.sex = new_client.sex;
            }
            if (client.birthday != new_client.birthday)
            {
                client.birthday= new_client.birthday;
                db_client.birthday = new_client.birthday;
            }
            if (client.telephone != new_client.telephone)
            {
                client.telephone = new_client.telephone;
                db_client.telephone = new_client.telephone;
            }
            if (client.email != new_client.email)
            {
                client.email = new_client.email;
                db_client.email = new_client.email;
            }

            db.Clients.Update(db_client);
            db.SaveChanges();
            this.HttpContext.Session.Remove("client");
            this.HttpContext.Session.Set("client", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(db_client)));
            return View(client);
        }

        // GET: UserController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
