namespace CinemaInfrastructure.Hubs
{
    public class BookingNotificationViewModel
    {
        public string FilmName { get; set; } = string.Empty;
        public string SeatInfo { get; set; } = string.Empty;
        public string SessionTime { get; set; } = string.Empty;
        public string ViewerName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("HH:mm:ss");
        // Added
        public string Operation { get; set; } = "Created"; // "Created", "Updated", "Deleted"
        public int? ViewerId { get; set; }
        public int? SessionId { get; set; }
        public int? SeatId { get; set; }
    }
}
