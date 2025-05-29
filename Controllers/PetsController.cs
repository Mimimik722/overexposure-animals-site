using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Project_site.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;

namespace Project_site.Controllers
{
    [Authorize]
    public class PetsController : Controller
    {
        readonly ApplicationContext db = ApplicationContext.GetInstance();

        //Получение информации о своих питомцах
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                ICollection<PetModel> pets = [.. db.Pets.Where(o => o.Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone) && o.Is_deleted != 1).Include(c => c.Breed_)];
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
                ViewBag.Breeds = db.Breeds.ToArray();
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
                Encryption enc = new();

                if (!pet.Name.ToString().All(char.IsLetter) ||
                    !pet.Name.ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("Name", "Имя животного не должно содержать спец. символы или цифры");
                    return View(pet);
                }
                if (!db.Breeds.Any(o => o.Name == Request.Form["breed"].ToString()))
                {
                    ModelState.AddModelError("Breed", "Такой породы не существует");
                    return View(pet);
                }
                if (pet.Age <= 0)
                {
                    ModelState.AddModelError("Age", "Возраст не может быть равен 0 или меньше");
                    return View(pet);
                }
                if (pet.Weight <= 0)
                {
                    ModelState.AddModelError("Weight", "Вес не может быть равен 0 или меньше");
                    return View(pet);
                }

                pet.Id = db.Pets.Count() + 1;
                if (Request.Form["radioM"] == "on")
                {
                    pet.Sex = "м";
                }
                else
                {
                    pet.Sex = "ж";
                }
                
                if (Request.Form.Files["Image"] != null)
                {
                    pet.Image = enc.ImageToByteString(Request.Form.Files["Image"]);
                }
                pet.Breed_ = db.Breeds.FirstOrDefault(o => o.Name == Request.Form["breed"].ToString());
                pet.Client_ = db.Users.Find(JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user")).Id); ;
                pet.Is_deleted = 0;

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
            if (db.Pets.FirstOrDefault(o => o.Id == pet).Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    ViewData["Breeds"] = db.Breeds.ToList();
                    PetModel? petM = db.Pets.FirstOrDefault(o => o.Id == pet);
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
        public IActionResult Edit(PetModel pet)
        {
            try
            {
                Encryption enc = new();
                if (!pet.Name.ToString().All(char.IsLetter) ||
                    !pet.Name.ToString().All(char.IsLetter))
                {
                    ModelState.AddModelError("Name", "Имя животного не должно содержать спец. символы или цифры");
                    return View(pet);
                }
                
                if (!db.Breeds.Any(o => o.Name == Request.Form["breed"].ToString()))
                {
                    ModelState.AddModelError("Breed", "Такой породы не существует");
                    return View(pet);
                }
                
                if (pet.Age <= 0)
                {
                    ModelState.AddModelError("Age", "Возраст не может быть равен 0 или меньше");
                    return View(pet);
                }
                
                if (pet.Weight <= 0)
                {
                    ModelState.AddModelError("Weight", "Вес не может быть равен 0 или меньше");
                    return View(pet);
                }

                PetModel old_pet = db.Pets.Where(o => o.Id == (int) TempData["pet"]).Include(o => o.Breed_).Include(o => o.Client_).First();
                old_pet.Breed_ = db.Breeds.First(o => o.Name == Request.Form["breed"].ToString());
                old_pet.Age = pet.Age;
                old_pet.Weight = pet.Weight;
                old_pet.Name = pet.Name;
                old_pet.Features = pet.Features;
                if (Request.Form["radioM"] == "on")
                {
                    old_pet.Sex = "м";
                }
                else
                {
                    old_pet.Sex = "ж";
                }
                if (Request.Form.Files != null)
                {
                    old_pet.Image = enc.ImageToByteString(Request.Form.Files.First());
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
            if (db.Pets.FirstOrDefault(o => o.Id == pet).Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    PetModel petM = db.Pets.First(o => o.Id == pet);
                    petM.Is_deleted = 1;
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
            if (db.Pets.Include(o => o.Client_).FirstOrDefault(o => o.Id == pet).Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
            {
                try
                {
                    ViewData["Breeds"] = db.Breeds.ToList();
                    PetModel? petM = db.Pets.FirstOrDefault(o => o.Id == pet);
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

        [HttpGet]
        public async Task<IActionResult> Image(int pet)
        {
            try
            {
                Encryption enc = new();
                FileStream fileStream = System.IO.File.OpenRead("wwwroot/images/paw.jpg");
                byte[] ImageBytes = new byte[int.Parse(fileStream.Length.ToString())];
                await fileStream.ReadAsync(ImageBytes, 0, int.Parse(fileStream.Length.ToString()));
                fileStream.Close();
                
                ICollection<PetModel> pets = [.. db.Pets.Where(o => o.Client_.Telephone == User.FindFirstValue(ClaimTypes.MobilePhone))];
                if (pet < 1 || pets.Count > pet || pets.ElementAt(pet - 1).Image == null)
                {
                    return File(ImageBytes, "image/jpg");
                }
                else
                {
                    ImageBytes = pets.ElementAt(pet - 1).Image;
                    return File(ImageBytes, "image/jpg");
                }
            }
            catch
            {
                return StatusCode(504);
            }
        }
	}
}
