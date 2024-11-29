using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Project_site.Models;

namespace Project_site.Controllers
{
    public class OrdersController : Controller
    {
        [HttpGet]
        [Authorize(Roles = "Sitter")]
        public IActionResult Index()
        {
			try
			{
				ApplicationContext db = new();
				UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
				SitterModel sitter = db.Sitters.FirstOrDefault(o => o.user_.id == user.id);
				ICollection<OrderModel> orders = db.Orders.Where(o => o.Sitter_ == sitter && o.Status == "Выполняется").Include(o => o.Client_).Include(o => o.Pet_).Include(o => o.Order_Type_).ToArray();
				return View(orders);
			}
			catch
			{
				return StatusCode(504);
			}
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult Create(int? sitter_id = -1, int? pet_id = -1)
        {
            try
            {
				ApplicationContext db = new();
				UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
				user = db.Users.Where(o => o.id == user.id).Include(o => o.town_).First();
				OrderModel order = new();

				if (sitter_id == -1)
				{
					ViewData["sitters"] = db.Sitters.Where(o => o.user_.town_ == user.town_).Include(o => o.user_);
					return View(order);
				}

				order.Sitter_ = db.Sitters.Where(o => o.id == sitter_id).Include(o => o.user_).First();
				TempData["Sitter"] = order.Sitter_.id;

				if (pet_id == -1)
				{
					ViewData["pets"] = db.Pets.Where(o => o.client_ == user).Include(o => o.breed_);
					return View(order);
				}

				order.Pet_ = db.Pets.Where(o => o.id == pet_id).Include(o => o.breed_).First();
				TempData["Pet"] = order.Pet_.id;
				ViewData["order_types"] = db.Order_types.ToArray();
				return View(order);
/*				if (order.Id == null)
				{
				}*/
	
			}
			catch
			{
				return StatusCode(504);
			}
        }

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "User")]
		public IActionResult Create(OrderModel order)
		{
			try
			{
				ApplicationContext db = new();
				UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
				user = db.Users.Where(o => o.id == user.id).Include(o => o.town_).First();

				if (order.Date_start.ToDateTime(TimeOnly.Parse("10:00 PM")) < DateTime.Now)
				{
					ModelState.AddModelError("Date_start", "Дата начала заказ не может быть раньше сегодняшнего дня");
					return View(order);
				}

				if (order.Date_end.ToDateTime(TimeOnly.Parse("10:00 PM")) < order.Date_start.ToDateTime(TimeOnly.Parse("10:00 PM")))
				{
					ModelState.AddModelError("Date_end", "Дата конца заказ не может быть раньше даты начала заказа");
					return View(order);
				}

				order.Sitter_ = db.Sitters.FirstOrDefault(s => s.id == (int) TempData["Sitter"]);
				order.Pet_ = db.Pets.FirstOrDefault(p => p.id == (int) TempData["Pet"]);
				order.Client_ = user;
				order.Id = db.Orders.Count() + 1;
				order.Order_Type_ = db.Order_types.FirstOrDefault(o => o.Name == Request.Form["Order_type_name"].ToString());
				order.Status = "В ожидании принятия";
				db.Orders.Add(order);
				db.SaveChanges();
				return RedirectToAction("Index", "Home");
			}
			catch
			{
				return StatusCode(504);
			}
		}

        [HttpGet]
        [Authorize(Roles = "Sitter")]
        public IActionResult New()
        {
			try
			{
				ApplicationContext db = new();
				UserModel user = JsonConvert.DeserializeObject<UserModel>(HttpContext.Session.GetString("user"));
				SitterModel sitter = db.Sitters.FirstOrDefault(o => o.user_.id == user.id);
				ICollection<OrderModel> orders = db.Orders.Where(o => o.Sitter_ == sitter && o.Status == "В ожидании принятия").Include(o => o.Client_).Include(o => o.Pet_).Include(o => o.Order_Type_).ToArray();
				return View(orders);
			}
			catch
			{
				return StatusCode(504);
			}
        }

		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Reject(int order_id)
		{
			try
			{
				ApplicationContext db = new();
				OrderModel order = db.Orders.FirstOrDefault(o => o.Id == order_id);
				order.Status = "Отменён";
				db.Orders.Update(order);
				db.SaveChanges();
				return RedirectToAction("New");
			}
			catch
			{
				return StatusCode(504);
			}
		}

		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Accept(int order_id)
		{
			try
			{
				ApplicationContext db = new();
				OrderModel order = db.Orders.FirstOrDefault(o => o.Id == order_id);
				order.Status = "Выполняется";
				db.Orders.Update(order);
				db.SaveChanges();
				return RedirectToAction("New");
			}
			catch
			{
				return StatusCode(504);
			}
		}

		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Done(int order_id)
		{
			try
			{
				ApplicationContext db = new();
				OrderModel order = db.Orders.FirstOrDefault(o => o.Id == order_id);
				order.Status = "Выполнен";
				db.Orders.Update(order);
				db.SaveChanges();
				return RedirectToAction("New");
			}
			catch
			{
				return StatusCode(504);
			}
		}

	}
}
