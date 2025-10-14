using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class FilmRating: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int ViewerId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int FilmId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Range(1, 10, ErrorMessage = "Оцінка має бути в діапазоні від 1 до 10!")]
    public int? Rating { get; set; }

    public virtual Film Film { get; set; } = null!;

    public virtual Viewer Viewer { get; set; } = null!;
}
