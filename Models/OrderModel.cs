namespace Project_site.Models
{
    public class OrderModel
    {
        public int? Id { get; set; }
        public SitterModel? Sitter_ {  get; set; }
        public UserModel? Client_ { get; set; }
        public PetModel? Pet_ { get; set; }
        public Feedback? Feedback_ { get; set; }
        public OrderType? Order_Type_ { get; set; }
        public DateOnly Date_start { get; set; }
        public DateOnly Date_end { get; set; }
        public string? Status {  get; set; }
    }
}
