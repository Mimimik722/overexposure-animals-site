using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Protocol;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Project_site.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Project_site.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientAPIController : ControllerBase
    {
        ApplicationContext db = ApplicationContext.GetInstance();

        private string GetHash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }

        [HttpGet("{id}")]
        public IResult Get(int id)
        {
            try
            {
                UserModel client = db.Users.Find(id);
                return Results.Json(client);
            }
            catch (Exception ex)
            {
                return Results.Json(ex);
            }
        }

        // POST api/<ClientAPIController>
        [HttpPost]
        public void Post([FromBody] JsonObject json)
        {
            var request = JsonConvert.DeserializeObject<UserModel>(json.ToString());
            try
            {
                if (!request.Name.ToString().All(char.IsLetter) ||
                    !request.Surname.ToString().All(char.IsLetter))
                {
                    Response.WriteAsync("Имя или Фамилия не должны содержать спец. символы или цифры");
                    return;
                }

                if (!db.Towns.Any(o => o.Name == request.Town_.Name.ToString()))
                {
                    Response.WriteAsJsonAsync("Указанного города не существует");
                    return;
                }

                if (db.Users.Any(o => o.Telephone == request.Telephone.ToString()))
                {
                    Response.WriteAsJsonAsync("Пользователь с таким номером телефона уже зарергистрирован");
                    return;
                }

                UserModel client = new UserModel
                {
                    Id = db.Users.Count() + 1,
                    Town_ = db.Towns.Where(o => o.Name == request.Town_.Name).First(),
                    Name = request.Name.ToString(),
                    Surname = request.Surname.ToString(),
                    Password = GetHash(request.Password.ToString()),
                    Email = request.Email.ToString()
                };

                if (request.Sex.ToString() == "м" || request.Sex.ToString() == "ж")
                {
                    client.Sex = request.Sex.ToString();
                }
                else
                {
                    Response.WriteAsJsonAsync("Указанного пола не существует");
                    return;
                }

                client.Telephone = request.Telephone.ToString();
                client.Birthday = DateOnly.Parse(request.Birthday.ToString());
                db.Users.Add(client);
                db.SaveChanges();
                Response.WriteAsJsonAsync(json);
            }
            catch (Exception ex)
            {
                Response.WriteAsync(ex.ToString());
            }
        }

        // PUT api/<ClientAPIController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {

        }

        // DELETE api/<ClientAPIController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
