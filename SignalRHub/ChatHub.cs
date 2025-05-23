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
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Debug.WriteLine("Client disconnected: " + Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
        
        public override Task OnConnectedAsync()
        {
            Debug.WriteLine("Client connected: " + Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        //Создание группы для каждой пары пользователей
        public void SetUserChatGroup(string ChatId)
        {
            var id = Context.ConnectionId;
            Debug.WriteLine($"Client {id} added to group " + ChatId);
            Groups.AddToGroupAsync(Context.ConnectionId, ChatId);
        }

        //Отправка сообщения всем пользователям в группе
        public async Task SendMessageToUserGroup(string ChatId, int senderId, string senderName, int receiverId, string message)
        {
            //Добавление сообщения в БД, после отправка всем
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var db = ApplicationContext.GetInstance();
            UserChatHistory chatHistory = new()
            {
                Message = message,
                Created_at = DateTime.UtcNow,
                Sender_ = db.Users.FirstOrDefault(o => o.Id == senderId),
                Receiver_ = db.Users.FirstOrDefault(o => o.Id == receiverId)
            };
            await db.UserChatHistory.AddAsync(chatHistory);
            await db.SaveChangesAsync();
            await Clients.Group(ChatId).SendAsync("ReceiveMessage", senderName, message);
        }   
    }
}