using System;

namespace CinemaInfrastructure.Models
{
    public class BookingIndexViewModel
    {
        public int ViewerId { get; set; }
        public int SessionId { get; set; }
        public int SeatId { get; set; }

        public string FilmName { get; set; } = string.Empty;
        public DateTime SessionTime { get; set; }
        public int SeatRow { get; set; }
        public int SeatNumberInRow { get; set; }
        public string ViewerName { get; set; } = string.Empty;

        public bool IsPast => SessionTime < DateTime.Now;
    }
}
