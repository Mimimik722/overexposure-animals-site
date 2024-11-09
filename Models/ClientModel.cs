namespace Project_site.Models
{
    public class ClientModel
    {
        public string? town { get; set; }
        public string? name { get; set; }
        public string? surname { get; set; }
        public string? sex { get; set; }
        public DateOnly? birthday { get; set; }
        public string? telephone { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? password_repeat { get; set; }
    }
}
