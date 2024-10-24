namespace Project_site.Models
{
    public class ClientModel
    {
        public int? id { get; set; }
        public int? town { get; set; }
        public string? name { get; set; }
        public string? surname { get; set; }
        public string? sex { get; set; }
        public DateOnly? birthday { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? password_repeat { get; set; }

        public ICollection<Order>? orders { get; set; }
    }
}
