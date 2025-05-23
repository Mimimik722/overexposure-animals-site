using Microsoft.AspNetCore.Mvc;

namespace Project_site.Models
{
    [Bind(include: "town_, name, surname, sex, birthday, telephone, email, image")]
    public class UserModel
    {
        public int Id { get; set; } = 0;
        public Town? Town_ { get; set; } = default;
        public string? Name { get; set; } = null;
        public string? Surname { get; set; } = string.Empty;
        public string? Sex { get; set; } = string.Empty;
        public DateOnly Birthday { get; set; } = new DateOnly();
        public string? Telephone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public Role? Role_ { get; set; } = default;
        public string? Password { get; set; } = string.Empty;
        public byte[]? Image { get; set; } = null!;
        public ICollection<PetModel>? Pets { get; set; } = null!;
    }
}
