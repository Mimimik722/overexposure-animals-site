using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Project_site.Models;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        public IActionResult Login(IFormCollection collection)
        {
            return View();
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
            catch (Exception ex)
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
                if (!db.Towns.Any(o => o.name == Request.Form["town"].ToString()))
                {
                    ModelState.AddModelError("town", "Такого города не существует");
                    return View(clientM);
                }
                if (Request.Form["password"] != Request.Form["password_repeat"])
                {
                    ModelState.AddModelError("password_repeat", "Пароли не совпадают");
                    return View(clientM);
                }
                try
                {
                    Client client = new Client();
                    client.id = db.Clients.Count() + 1;
                    client.town_id = db.Towns.Where(o => o.name == Request.Form["town"].ToString()).First().id;
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
                    client.telephone = Request.Form["phone"];
                    client.birthday = DateOnly.Parse(Request.Form["birthday"].ToString());
                    db.Clients.Add(client);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Error");
                }
               
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home");
            }
            return RedirectToAction("Login");
        }

        // GET: UserController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
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

        // GET: UserController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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
