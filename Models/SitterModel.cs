namespace Project_site.Models
{
    public class SitterModel
    {
        public int? Id { get; set; }
        public UserModel User_ { get; set; } = null!;
        public float? Payment { get; set; }
        public int? Experience { get; set; }
        public int Is_verificated { get; set; } = 0;
        public string Status { get; set; }

        public ICollection<OrderModel>? Orders { get; set; }
    }
}
