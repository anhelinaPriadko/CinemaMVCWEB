using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CinemaDomain.Model;

public partial class Company: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [StringLength(30, ErrorMessage = "Назва виробника не може перевищувати 30 символів!")]
    public string Name { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Film> Films { get; set; } = new List<Film>();
}
