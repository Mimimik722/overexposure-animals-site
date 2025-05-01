using System.ComponentModel.DataAnnotations.Schema;

namespace Project_site.Models
{
    public class PetModel
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public UserModel client_ { get; set; } = null!;
        public Breed breed_ { get; set; } = null!;
        public string sex { get; set; } = "м";
        public int age { get; set; } = -1;
        public float? weight { get; set; } = -1;
        public string? features { get; set; }
        public int? is_deleted { get; set; }
    }
}
