using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
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
				ICollection<string> statuses = [];
				orders = db.Orders.Where(o => o.Client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone) || o.Sitter_ == sitter)
					//&& db.OrdersHistory.FirstOrDefault(p => p.order_.Id == o.Id).action != "В ожидании принятия")
					.Include(o => o.Sitter_.user_)
					.Include(o => o.Client_)
					.Include(o => o.Pet_)
					.Include(o => o.Order_Type_)
					.Include(o => o.Feedback_)
					.ToArray();
				foreach (var order in orders)
				{
					statuses.Add(db.OrdersHistory.Where(o => o.order_.Id == order.Id).OrderByDescending(o => o.timestamp).FirstOrDefault().action);
				}
				ViewData["Statuses"] = statuses;
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
        public IActionResult Create(int? sitter = -1, int? pet = -1, int? payment_from = 0, int? payment_to = 1000000, float? rating_from = 0, float? rating_to = 10)
        {
            try
            {
				OrderModel order = new();

				if (pet == -1 || db.Pets.FirstOrDefault(o => o.id == pet).client_.telephone != User.FindFirstValue(ClaimTypes.MobilePhone))
				{
					ViewData["pets"] = db.Pets.Where(o => o.client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)).Include(o => o.breed_);
					return View(order);
				}
				
				order.Pet_ = db.Pets.Where(o => o.id == pet).Include(o => o.breed_).First();
				TempData["Pet"] = order.Pet_.id;
				
				if (sitter == -1 || db.Sitters.FirstOrDefault(o => o.id == sitter).user_.town_.name != User.FindFirstValue("Town"))
				{
					ICollection<Requirement> requirements = [];
					if (TempData["filter"] != null)
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
						& o.Sitter_.status == "Свободен"
						& o.Sitter_.is_verificated == 1).Include(o => o.Sitter_.user_).ToList();
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

				order.Sitter_ = db.Sitters.Where(o => o.id == sitter).Include(o => o.user_).First();
				TempData["Sitter"] = order.Sitter_.id;

				ViewData["order_types"] = db.Order_types.ToArray();
				return View(order);
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
				OrdersHistory ordersHistory = new();
				ordersHistory.order_ = order;
				ordersHistory.timestamp = DateTime.Now;
				ordersHistory.action = "В ожидании принятия";
				db.Orders.Add(order);
				db.OrdersHistory.Add(ordersHistory);
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
				//db.OrdersHistory.GroupBy(o => o.order_).Select(g => new { order = g.Key, count = g.Count() }).Where(o => o.order.Sitter_ == sitter && o.count == 1);
				ICollection<OrderModel> orders = db.Orders.Where(o => o.Sitter_ == sitter)
					.Include(o => o.Pet_)
					.Include(o => o.Order_Type_)
					.Include(o => o.Client_)
					.Join(
						db.OrdersHistory.GroupBy(o => o.order_).Select(g => new { order = g.Key, count = g.Count() }), 
						orders => orders.Id, 
						ordersHistory => ordersHistory.order.Id, 
						(orders, ordersHistory) => new {Orders = orders, OrdersHistory = ordersHistory})
					.Where(o => o.OrdersHistory.count == 1)
					.Select(o => o.Orders)
					.ToArray();
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
		public IActionResult Reject(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)
					&& db.OrdersHistory.Where(o => o.order_.Id == order).Count() == 1)
				{
					OrdersHistory ordersHistory = new();
					ordersHistory.order_ = db.Orders.FirstOrDefault(o => o.Id == order);
					ordersHistory.timestamp = DateTime.Now;
					ordersHistory.action = "Отменён";
					db.OrdersHistory.Add(ordersHistory);
					db.SaveChanges();
					return RedirectToAction("New");
				}
				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
		}

		//Принятие заказа
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Accept(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)
					&& db.OrdersHistory.Where(o => o.order_.Id == order).Count() == 1)
				{
					OrdersHistory ordersHistory = new();
					ordersHistory.order_ = db.Orders.FirstOrDefault(o => o.Id == order);
					ordersHistory.timestamp = DateTime.Now;
					ordersHistory.action = "Выполняется";
					db.OrdersHistory.Add(ordersHistory);
					db.SaveChanges();
					return RedirectToAction("New");
				}

				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
		}

		//Подтверждения выполнения заказа
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult Done(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)
					&& db.OrdersHistory.Where(o => o.order_.Id == order).Any(o => o.action != "Выполнено"))
				{
					OrdersHistory ordersHistory = new();
					ordersHistory.order_ = db.Orders.FirstOrDefault(o => o.Id == order);
					ordersHistory.timestamp = DateTime.Now;
					ordersHistory.action = "Выполнен";
					db.OrdersHistory.Add(ordersHistory);
					db.SaveChanges();
					return RedirectToAction("Index");
				}
				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
		}
		//Вывод окна с вводом текущего действия с питомцем
		[HttpGet]
		[Authorize(Roles = "Sitter")]
		public IActionResult NewAction(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)
					&& db.OrdersHistory.Where(o => o.order_.Id == order).Count() > 1
					&& db.OrdersHistory.Where(o => o.order_.Id == order).Any(o => o.action == "Отменён"))
				{
					TempData["order_id"] = order;
					OrdersHistory orderHistory = new();
					return View(orderHistory);
				}
				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
		}
		//Добавление нового действия с питомцем
        [HttpPost]
        [Authorize(Roles = "Sitter")]
        public IActionResult NewAction(OrdersHistory ordersHistory)
        {
			if (ordersHistory.action == "")
			{
				ModelState.AddModelError("action", "Поле не может быть пустым");
				return View(ordersHistory);
			}
			try
			{
				ordersHistory.order_ = db.Orders.FirstOrDefault(o => o.Id == int.Parse(TempData["order_id"].ToString()));
				ordersHistory.timestamp = DateTime.Now;
				db.OrdersHistory.Add(ordersHistory);
				db.SaveChanges();
			}
			catch
			{
				return StatusCode(504);
			}
            return RedirectToAction("Index", "Orders");
        }

		[HttpGet]
		[Authorize]
		public IActionResult Actions(int order)
		{
			try
			{
				if (db.Orders.Include(o => o.Sitter_.user_).FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone).ToString()
					|| db.Orders.Include(o => o.Client_).FirstOrDefault(o => o.Id == order).Client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone).ToString())
				{
					ICollection <OrdersHistory> orderHistory = db.OrdersHistory.Where(o => o.order_.Id == order).OrderBy(o => o.timestamp).ToArray();
					return View(orderHistory);
				}
				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
		}

        //Вывод страницы с отзывом о заказе
        [HttpGet]
		[Authorize(Roles = "User")]
		public IActionResult Feedback(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
				{
					TempData["order_id"] = order;
					return View();
				}
				else
				{
					return RedirectToAction("Index");
				}
			}
			catch
			{
				return StatusCode(504);
			}
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
		[Authorize]
		public IActionResult Map(int order)
		{
			try
			{
				if (db.Orders.FirstOrDefault(o => o.Id == order).Sitter_.user_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone)
					|| db.Orders.FirstOrDefault(o => o.Id == order).Client_.telephone == User.FindFirstValue(ClaimTypes.MobilePhone))
				{
					try
					{
						OrderModel? orderM = db.Orders.Find(order);
						Coordinate coordinate = db.Coordinates.Where(o => o.Order_ == orderM).OrderBy(o => o.timestamp).Last();
						coordinate.Order_ = null;
						return View(coordinate);
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
			catch
			{
				return StatusCode(504);
			}
		}
    }
}
