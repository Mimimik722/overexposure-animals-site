namespace Project_site.Models
{
    public class SitterModel
    {
        public int? id { get; set; }
        public UserModel user_ { get; set; } = null!;
        public float? payment { get; set; }
        public int? experience { get; set; }
        public int is_verificated { get; set; } = 0;
        public string status { get; set; }

        public ICollection<OrderModel>? orders { get; set; }
    }
}
