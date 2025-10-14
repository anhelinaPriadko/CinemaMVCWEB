using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Booking
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int ViewerId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int SessionId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int SeatId { get; set; }

    public virtual Seat Seat { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;

    public virtual Viewer Viewer { get; set; } = null!;
}
