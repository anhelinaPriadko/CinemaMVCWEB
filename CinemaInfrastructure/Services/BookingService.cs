using CinemaDomain.Model;
using CinemaInfrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CinemaInfrastructure.Services
{
    public class BookingService
    {
        private readonly IHubContext<BookingHub> _hubContext;

        public BookingService(IHubContext<BookingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendBookingNotificationAsync(Booking booking, string operation = "Created")
        {
            if (booking == null)
                return;

            var update = new BookingNotificationViewModel
            {
                FilmName = booking.Session?.Film?.Name ?? "",
                SeatInfo = booking.Seat != null ? $"Ряд {booking.Seat.Row}, Місце {booking.Seat.NumberInRow}" : "N/A",
                SessionTime = booking.Session?.SessionTime.ToString("dd.MM HH:mm") ?? "",
                ViewerName = booking.Viewer?.Name ?? "",
                Operation = operation,
                ViewerId = booking.ViewerId,
                SessionId = booking.SessionId,
                SeatId = booking.SeatId
            };

            try
            {
                Console.WriteLine($"[BookingService] Sending {operation} update: {update.FilmName} | {update.SeatInfo} | ids: {update.ViewerId}/{update.SessionId}/{update.SeatId}");
                await _hubContext.Clients.All.SendAsync("ReceiveBookingUpdate", update);
                Console.WriteLine("[BookingService] SendAsync completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[BookingService] ERROR sending update: " + ex);
            }
        }

    }
}
