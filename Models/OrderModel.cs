namespace Project_site.Models
{
    public interface OrderModel
    {
        int Id { get; set; }
        SitterModel Sitter_ {  get; set; }
        UserModel Client_ { get; set; }
        PetModel Pet_ { get; set; }
        Feedback Feedback_ { get; set; }
        OrderType Order_Type_ { get; set; }
        DateOnly Date_start { get; set; }
        DateOnly Date_end { get; set; }
    }
}
