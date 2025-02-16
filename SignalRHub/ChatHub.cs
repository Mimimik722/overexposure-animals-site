using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Project_site.Models;
using Microsoft.AspNetCore.Authorization;

namespace Project_site.SignalRHub
{
    [AllowAnonymous]
    public class ChatHub : Hub
    {
        public override Task OnDisconnectedAsync(Exception exception)
        {
            Debug.WriteLine("Client disconnected: " + Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
        public override Task OnConnectedAsync()
        {
            Debug.WriteLine("Client connected: " + Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        //Create Group for each user to chat sepeartely
        public void SetUserChatGroup(string userChatId)
        {
            var id = Context.ConnectionId;
            Debug.WriteLine($"Client {id} added to group " + userChatId);
            Groups.AddToGroupAsync(Context.ConnectionId, userChatId);
        }

        //Create a group for each user to chat separately for private conversations.
        public void CreateUserChatGroup(int userId)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            Debug.WriteLine($"Client {Context.ConnectionId} added to group " + userId);
            //Adds the client associated with the current connection ID to a specified group.In this case, we are taking `userId` as a parameter for this function,
            //Where `userId` represents the name of the group for loggedin user.
            //For each user in our database, we create a unique group with ConnectionId
            //This function is used to add a client to a specific chat group, enabling them to participate in conversations within that group. 

            //We have written this logic, to  ensuring that each user only gets messages for their group
            //identified by `userId`. that means logged-in users will only receive messages
            //sent to their groups, enhancing privacy and ensuring that users do not receive messages for other users to make sure private chat.
        }

        //Send message to user Group
        public async Task SendMessageToUserGroup(int senderId, string senderName, int receiverId, string message)
        {
            //Insert message to database then send it to the Client
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var db = new ApplicationContext();
            UserChatHistory chatHistory = new UserChatHistory();
            chatHistory.id = db.UserChatHistory.Count() + 1;
            chatHistory.message = message;
            chatHistory.created_at = DateTime.UtcNow;
            chatHistory.sender_ = db.Users.FirstOrDefault(o => o.id == senderId);
            chatHistory.receiver_ = db.Users.FirstOrDefault(o => o.id == receiverId);
            await db.UserChatHistory.AddAsync(chatHistory);
            await db.SaveChangesAsync();
            //await Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", senderId, senderName, message);
            //"Send the message to all users in the specified group. We take the sender's user ID and the receiver's user ID, and then send the message to
            //the group of the receiver's user ID to ensure that only users within the specified receiver group receive the message.
            //This allows for private communication between users in a one-to-one chat system."
        }
    }
}