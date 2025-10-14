using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Seat: Entity
{
    public int HallId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер ряду не повинен бути меншим за 1!")]
    public int Row { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер місця не повинен бути меншим за 1!")]
    public int NumberInRow { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Hall Hall { get; set; } = null!;
}
