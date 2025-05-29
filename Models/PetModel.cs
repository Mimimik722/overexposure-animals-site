using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_site.Models
{
    [Bind("Name, Breed_, Image, Sex, Age, Weight, Features")]
    public class PetModel
    {
        public int? Id { get; set; } = -1;
        public string? Name { get; set; } = "";
        public UserModel Client_ { get; set; } = null!;
        public Breed Breed_ { get; set; } = null!;
        [MaxLength(1000000)]
        public byte[]? Image { get; set; } = null!;
        public string? Sex { get; set; } = "м";
        public int? Age { get; set; } = -1;
        public float? Weight { get; set; } = -1;
        public string? Features { get; set; } = "";
        public int? Is_deleted { get; set; } = -1;
    }
}
