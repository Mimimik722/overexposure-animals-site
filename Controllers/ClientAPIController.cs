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
                ApplicationContext db = new ApplicationContext();
                ClientModel client = db.Clients.Find(id);
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
            var request = JsonConvert.DeserializeObject<Client>(json.ToString());
            try
            {
                ApplicationContext db = new ApplicationContext();

                if (!request.name.ToString().All(char.IsLetter) ||
                    !request.surname.ToString().All(char.IsLetter))
                {
                    Response.WriteAsync("Имя или Фамилия не должны содержать спец. символы или цифры");
                    return;
                }

                if (!db.Towns.Any(o => o.id == int.Parse(request.town_id.ToString())))
                {
                    Response.WriteAsJsonAsync("Указанного города не существует");
                    return;
                }

                if (db.Clients.Any(o => o.telephone == request.telephone.ToString()))
                {
                    Response.WriteAsJsonAsync("Пользователь с таким номером телефона уже зарергистрирован");
                    return;
                }

                ClientModel client = new ClientModel();

                client.id = db.Clients.Count() + 1;
                client.town_id = int.Parse(request.town_id.ToString());
                client.name = request.name.ToString();
                client.surname = request.surname.ToString();
                client.password = GetHash(request.password.ToString());
                client.email = request.email.ToString();

                if (request.sex.ToString() == "м" || request.sex.ToString() == "ж")
                {
                    client.sex = request.sex.ToString();
                }
                else
                {
                    Response.WriteAsJsonAsync("Указанного пола не существует");
                    return;
                }

                client.telephone = request.telephone.ToString();
                client.birthday = DateOnly.Parse(request.birthday.ToString());
                db.Clients.Add(client);
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
