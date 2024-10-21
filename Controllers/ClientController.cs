using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace Project_site.Controllers
{
    public class ClientController : Controller
    {

        // GET: UserController/Login
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
        public IActionResult Registration(IFormCollection collection)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                Client client = new Client();
                client.id = db.Clients.Count()+1;
                client.town_id = int.Parse(collection["town_id"].ToString());
                client.name = collection["name"].ToString();
                client.password = collection["password"].ToString();
                client.email = collection["email"].ToString();
                client.phone = collection["phone"].ToString();
                client.birthday = new DateOnly(int.Parse(collection["yearState"].ToString()), int.Parse(collection["monthState"].ToString()), int.Parse(collection["dayState"].ToString()));
                /*using (ApplicationContext db = new ApplicationContext())
                {
                    User user = new 
                }*/
                db.Clients.Add(client);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home");
            }
            return View();
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
