using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Hall: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [StringLength(30, ErrorMessage = "Назва залу не може перевищувати 30 символів!")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(1, 20, ErrorMessage = "Кількість рядів повинна бути від 1 до 20!")]
    public int NumberOfRows { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(1, 25, ErrorMessage = "Кількість місць повинна бути від 1 до 25!")]
    public int SeatsInRow { get; set; }
    
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int HallTypeId { get; set; }

    public virtual HallType HallType { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
