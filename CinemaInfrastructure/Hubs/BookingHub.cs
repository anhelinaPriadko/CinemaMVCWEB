using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Hubs
{
    public class BookingHub: Hub
    {
        // Метод, який викликається з BookingService для push-сповіщення
        public async Task SendBookingUpdate(BookingNotificationViewModel update)
        {
            // "ReceiveBookingUpdate" — функція, яку слухає клієнтський JavaScript
            await Clients.All.SendAsync("ReceiveBookingUpdate", update);
        }

        // Методи для моніторингу (C10)
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"[BookingHub] Connected: {Context.ConnectionId} (User: {Context.UserIdentifier ?? "anon"})");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Клієнт відключився: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
