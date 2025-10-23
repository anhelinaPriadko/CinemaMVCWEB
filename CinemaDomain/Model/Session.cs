using CinemaDomain.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CinemaDomain.Model;

public partial class Session: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int FilmId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int HallId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [MinHourFromNow(1, 2)]
    public DateTime SessionTime { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(90, 300, ErrorMessage = "Тривалість сеансу повинна бути від 90 до 300 хвилин!")]
    public int Duration { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Film? Film { get; set; }

    public virtual Hall? Hall { get; set; }
}
