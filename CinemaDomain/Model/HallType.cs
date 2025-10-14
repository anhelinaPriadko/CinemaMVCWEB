using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class HallType: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [StringLength(30, ErrorMessage = "Назва типу залу не може перевищувати 30 символів!")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
}
