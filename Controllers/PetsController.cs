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
    [Authorize]
    public class PetsController : Controller
    {
        public ApplicationContext db;
        public PetsController()
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

        //Получение информации о своих питомцах
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                int client_id = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user")).id;
                ICollection<PetModel> pets = db.Pets.Where(o => o.client_.id == client_id && o.is_deleted != 1).Include(c => c.breed_).ToArray();
                return View(pets);
            }
            catch
            {
                return StatusCode(504);
            }

        }

        //Переход на страницу для добавления нового животного
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                ViewBag.Breeds = db.Breeds.ToList();
            }
            catch
            {
                return StatusCode(504);
            }
            return View();
        }

        //Добавление нового животного
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Create(PetModel pet)
        {
            try
            {
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
                
                pet.breed_ = db.Breeds.FirstOrDefault(o => o.name == Request.Form["breed"].ToString());
                pet.client_ = db.Users.Find(JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user")).id); ;
                pet.is_deleted = 0;

                db.Pets.Add(pet);
                db.SaveChanges();
                
                return RedirectToAction("Index");
            }
            catch
            {
                return StatusCode(504);
            }
        }

        //Переход на страницу для изменения данных о животном
        [HttpGet]
        public IActionResult Edit(int pet)
        {
            if (db.Pets.FirstOrDefault(o => o.id == pet).client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    ViewData["Breeds"] = db.Breeds.ToList();
                    PetModel? petM = db.Pets.FirstOrDefault(o => o.id == pet);
                    TempData["pet"] = pet;
                    return View(petM);
                }
                catch
                {
                    return StatusCode(504);
                }
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        //Изменение данных о животном
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Edit(PetModel pet)
        {
            try
            {
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

                PetModel old_pet = db.Pets.Where(o => o.id == (int) TempData["pet"]).Include(o => o.breed_).Include(o => o.client_).First();
                old_pet.breed_ = db.Breeds.First(o => o.name == Request.Form["breed"].ToString());
                old_pet.age = pet.age;
                old_pet.weight = pet.weight;
                old_pet.name = pet.name;
                old_pet.features = pet.features;
                if (Request.Form["radioM"] == "on")
                {
                    old_pet.sex = "м";
                }
                else
                {
                    old_pet.sex = "ж";
                }

                db.Pets.Update(old_pet);
                db.SaveChanges();
                ViewData["Breeds"] = db.Breeds.ToList();
                return View(old_pet);
            }
            catch
            {
                return StatusCode(504);
            }
            
        }

        //Удаление животного
        public IActionResult Delete(int pet)
        {
            if (db.Pets.FirstOrDefault(o => o.id == pet).client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    PetModel petM = db.Pets.First(o => o.id == pet);
                    petM.is_deleted = 1;
                    db.Pets.Update(petM);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch
                {
                    return StatusCode(504);
                }
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        //Получение информации о животном
        [HttpGet]
		public IActionResult Details(int pet)
		{
            if (db.Pets.FirstOrDefault(o => o.id == pet).client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    ViewData["Breeds"] = db.Breeds.ToList();
                    PetModel? petM = db.Pets.FirstOrDefault(o => o.id == pet);
                    return View(pet);
                }
                catch
                {
                    return StatusCode(504);
                }
            }
            else
            {
                return RedirectToAction("Index");
            }
		}
	}
}
