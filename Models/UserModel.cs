namespace Project_site.Models
{
    public class UserModel
    {
        public int id { get; set; } = 0;
        public Town? town_ { get; set; } = default(Town);
        public string? name { get; set; } = null;
        public string? surname { get; set; } = string.Empty;
        public string? sex { get; set; } = string.Empty;
        public DateOnly birthday { get; set; } = new DateOnly();
        public string? telephone { get; set; } = string.Empty;
        public string? email { get; set; } = string.Empty;  
        public Role? role_ { get; set; } = default(Role);
        public string? password { get; set; } = string.Empty;
        public ICollection<PetModel>? Pets { get; set; } = null!;
    }
}
