using System.ComponentModel.DataAnnotations.Schema;

namespace Project_site.Models
{
    public class PetModel
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public ClientModel? client_ { get; set; }
        public Breed? breed_ { get; set; }
        public string? sex { get; set; }
        public int? age { get; set; }
        public float? weight { get; set; }
        public string? features { get; set; }
        public int? is_deleted { get; set; }
    }
}
