using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Project_site.Models;
using System.Linq;
using System.Security.Claims;

namespace Project_site.Controllers
{
    public class OrdersController : Controller
    {
		public ApplicationContext db;
		string telephone;
		public OrdersController()
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

		//Список текущих заказов
		[HttpGet]
        [Authorize(Roles = "Sitter, User")]
        public IActionResult Index()
        {
			try
			{
				SitterModel? sitter = db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
				ICollection<OrderModel> orders = [];
				orders = db.Orders.Where(o => o.Client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone) || o.Sitter_ == sitter).Include(o => o.Sitter_.user_).Include(o => o.Client_).Include(o => o.Pet_).Include(o => o.Order_Type_).Include(o => o.Feedback_).ToArray();
				return View(orders);
			}
			catch
			{
				return StatusCode(504);
			}
        }

		//Вывод страницы с новым заказом
        [HttpGet]
        [Authorize(Roles = "User, Sitter")]
        public IActionResult Create(int? sitter_id = -1, int? pet_id = -1, int? payment_from = 0, int? payment_to = 1000000, float? rating_from = 0, float? rating_to = 10)
        {
            try
            {
				OrderModel order = new();

				if (pet_id == -1)
				{
					ViewData["pets"] = db.Pets.Where(o => o.client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.breed_);
					return View(order);
				}
				
				order.Pet_ = db.Pets.Where(o => o.id == pet_id).Include(o => o.breed_).First();
				TempData["Pet"] = order.Pet_.id;
				
				if (sitter_id == -1)
				{
					ICollection<Requirement> requirements = [];
					if (TempData["sitters"] != null)
					{
						requirements = (ICollection<Requirement>) TempData["sitters"];
						Console.WriteLine(requirements);
					}
					else
					{
						requirements = db.Requirements.Include(o => o.Sitter_).Where(o => o.Sitter_.user_.town_.name == User.FindFirstValue("Town")
						& order.Pet_.weight >= o.weight_from & order.Pet_.weight <= o.weight_to
						& order.Pet_.age >= o.age_from & order.Pet_.age <= o.age_to
						& payment_from <= o.Sitter_.payment & payment_to >= o.Sitter_.payment
						& o.Sitter_.status == "Свободен").Include(o => o.Sitter_.user_).ToList();
					}
					ICollection<SitterModel> sitters = [];
					ICollection<float> ratings = [];
					foreach (Requirement requirement in requirements)
					{
						float rating = (float) db.Orders.Where(o => o.Sitter_ == requirement.Sitter_ && o.Feedback_ != null).Average(o => o.Feedback_.Rating);
                        if (rating > rating_from && rating <= rating_to)
						{
                            sitters.Add(requirement.Sitter_);
                            ratings.Add(rating);
                        }
					}
					ViewData["sitters"] = sitters;
					ViewData["ratings"] = ratings;
					return View(order);
				}

				order.Sitter_ = db.Sitters.Where(o => o.id == sitter_id).Include(o => o.user_).First();
				TempData["Sitter"] = order.Sitter_.id;

				ViewData["order_types"] = db.Order_types.ToArray();
				return View(order);
			}
			catch
			{
				return StatusCode(504);
			}
        }

		[HttpPost]
		public IActionResult Requirement(int pet_id)
		{
			try
			{
				TempData["sitters"] = db.Requirements.Include(o => o.Sitter_).Where(o => o.Sitter_.payment >= int.Parse(Request.Form["payment_from"])
				& o.Sitter_.payment <= int.Parse(Request.Form["payment_to"])).Include(o => o.Sitter_.user_).ToList();
				return RedirectToAction("Create");
			}
			catch
			{
				return StatusCode(504);
			}
		}

		//Создание заказа
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "User, Sitter")]
		public IActionResult Create(OrderModel order)
		{
			try
			{
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

				order.Sitter_ = db.Sitters.FirstOrDefault(s => s.id == (int)TempData["Sitter"]);
				order.Pet_ = db.Pets.FirstOrDefault(p => p.id == (int)TempData["Pet"]);
				order.Client_ = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
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

		//Просмотр новых заказов
        [HttpGet]
        [Authorize(Roles = "Sitter")]
        public IActionResult New()
        {
			try
			{
				SitterModel? sitter = db.Sitters.FirstOrDefault(o => o.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
				ICollection<OrderModel> orders = db.Orders.Where(o => o.Sitter_ == sitter && o.Status == "В ожидании принятия").Include(o => o.Client_).Include(o => o.Pet_).Include(o => o.Order_Type_).ToArray();
				return View(orders);
			}
			catch
			{
				return StatusCode(504);
			}
        }

		//Отклонение заказа
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Reject(int order_id)
		{
			try
			{
				OrderModel? order = db.Orders.FirstOrDefault(o => o.Id == order_id);
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

		//Принятие заказа
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Accept(int order_id)
		{
			try
			{
				OrderModel? order = db.Orders.FirstOrDefault(o => o.Id == order_id);
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

		//Подтверждения выполнения заказа
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Done(int order_id)
		{
			try
			{
				OrderModel? order = db.Orders.FirstOrDefault(o => o.Id == order_id);
				order.Status = "Выполнен";
				db.Orders.Update(order);
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			catch
			{
				return StatusCode(504);
			}
		}

		//Вывод страницы с отзывом о заказе
		[HttpGet]
		[Authorize(Roles = "User")]
		public IActionResult Feedback(int order_id)
		{
			TempData["order_id"] = order_id;
			return View();
		}

		//Отправка отзыва о заказе
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "User")]
		public IActionResult Feedback(Feedback feedback)
		{
			try
			{
				OrderModel? order = db.Orders.Include(o => o.Sitter_).Include(o => o.Pet_).Include(o => o.Client_).Include(o => o.Order_Type_).FirstOrDefault(o => o.Id == (int)TempData["order_id"]);
				feedback.id = db.Feedbacks.Count() + 1;
				feedback.Rating = int.Parse(Request.Form["ratings"]);
				order.Feedback_ = feedback;
				db.Feedbacks.Add(feedback);
				db.Update(order);
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			catch
			{
				return StatusCode(504);
			}
		}

		//Отображение положения питомца на карте
		[Authorize(Roles = "User, Sitter")]
		public IActionResult Map(int order_id)
		{
			try
			{
				OrderModel? order = db.Orders.Find(order_id);
				Coordinate coordinate = db.Coordinates.Where(o => o.Order_ == order).OrderBy(o => o.timestamp).Last();
				coordinate.Order_ = null;
                return View(coordinate);
			}
			catch
			{
				return StatusCode(504);
			}
		}

        public IActionResult Chat(int user_id)
        {
            UserChatModel userChat = new UserChatModel();

			int senderId = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).id;
            string? name = User.Identity.Name;

            userChat.LoggedInUser = new UserModel { id = senderId, name = name };

			userChat.Receiver = db.Users.FirstOrDefault(o => o.id == user_id);
            return View(userChat);
        }
        
		public ActionResult GetChatConversion(int receiverId)
        {
			UserModel user;
			user = db.Users.FirstOrDefault(o => o.telephone == User.FindFirstValue(ClaimTypes.MobilePhone));
			int loginUserId = user.id;
            var chatHistories = db.UserChatHistory.Include("sender_")
                                .Include("receiver_").Where(a => (a.receiver_ == user && a.sender_.id == receiverId)
                               || (a.receiver_.id == receiverId && a.sender_ == user)).OrderByDescending(a => a.created_at).ToList();
            ViewData["loginUserId"] = loginUserId;
            return PartialView("_ChatConversion", chatHistories);
        }
    }
}
