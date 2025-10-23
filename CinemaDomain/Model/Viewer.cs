using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CinemaDomain.Model;

public partial class Viewer: Entity
{
    public string Name { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string UserId { get; set; }

    public User? User { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [JsonIgnore]
    public virtual ICollection<FilmRating> FilmRatings { get; set; } = new List<FilmRating>();
}
