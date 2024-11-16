using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Project_site.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace Project_site.Controllers
{
    public class PetController : Controller
    {
        [Authorize]
        public IActionResult Index()
        {
            try
            {
                
                ApplicationContext db = new ApplicationContext();
                int client_id = JsonConvert.DeserializeObject<ClientModel>(HttpContext.Session.GetString("client")).id;
                ICollection<PetModel> pets = db.Clients.Include(c => c.Pets).Where(o => o.id == client_id).First().Pets;//Не сохраняется инфомрация о животных
                return View(pets);
            }
            catch
            {
                return StatusCode(503);
            }
            
        }
        [Authorize]
        public IActionResult Create()
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                @ViewBag.Breeds = db.Breeds.ToList();
            }
            catch
            {
                return StatusCode(503);
            }
            return View();
        }
        [Authorize]
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Create(PetModel pet)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                
                if (!pet.name.ToString().All(char.IsLetter) ||
                    !pet.name.ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("name", "Имя животного не должно содержать спец. символы или цифры");
                    return View(pet);
                }
                if (!db.Breeds.Any(o => o.name == Request.Form["breed"].ToString()))
                {
                    ModelState.AddModelError("breed", "Такой породы не существует");
                    return View(pet);
                }
                if (pet.age <= 0)
                {
                    ModelState.AddModelError("age", "Возраст не может быть равен 0 или меньше");
                    return View(pet);
                }
                if (pet.weight <= 0)
                {
                    ModelState.AddModelError("weight", "Вес не может быть равен 0 или меньше");
                    return View(pet);
                }

                pet.id = db.Pets.Count() + 1;
                if (Request.Form["radioM"] == "on")
                {
                    pet.sex = "м";
                }
                else
                {
                    pet.sex = "ж";
                }
                pet.breed_ = db.Breeds.Where(o => o.name == Request.Form["breed"].ToString()).First();
                ClientModel client = db.Clients.Find(JsonConvert.DeserializeObject<ClientModel>(HttpContext.Session.GetString("client")).id);
                pet.client_ = client;
                client.Pets.Add(pet);

                db.Pets.Add(pet);
                db.SaveChanges();
                
                return RedirectToAction("Index");
            }
            catch
            {
                return StatusCode(503);
            }
        }
        [Authorize]
        public IActionResult Edit(int id)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                ViewBag["Breeds"] = db.Breeds.ToList();
                PetModel pet = db.Pets.Find(id);
                return View(pet);
            }
            catch
            {
                return StatusCode(503);
            }
        }
        [Authorize]
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Edit(PetModel pet)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                if (!pet.name.ToString().All(char.IsLetter) ||
                    !pet.name.ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("name", "Имя животного не должно содержать спец. символы или цифры");
                    return View(pet);
                }
                if (!db.Breeds.Any(o => o.name == Request.Form["breed"].ToString()))
                {
                    ModelState.AddModelError("breed", "Такой породы не существует");
                    return View(pet);
                }
                if (pet.age <= 0)
                {
                    ModelState.AddModelError("age", "Возраст не может быть равен 0 или меньше");
                    return View(pet);
                }
                if (pet.weight <= 0)
                {
                    ModelState.AddModelError("weight", "Вес не может быть равен 0 или меньше");
                    return View(pet);
                }

                db.Pets.Update(pet);
                db.SaveChanges();
                return View(pet);
            }
            catch
            {
                return StatusCode(503);
            }
            
        }
        [Authorize]
        public IActionResult Delete(PetModel pet)
        {
            try
            {
                ApplicationContext db = new ApplicationContext();
                db.Pets.Remove(pet);
                return RedirectToAction("Index");
            }
            catch 
            {
                return StatusCode(503);
            }
        }
    }
}
