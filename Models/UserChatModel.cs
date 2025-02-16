namespace Project_site.Models
{
    public class UserChatModel
    {
        public UserModel LoggedInUser { get; set; } = null!;
        public UserModel Receiver { get; set; } = null!;
    }
}
