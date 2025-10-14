using System;
using System.Collections.Generic;

namespace CinemaDomain.Model;

public partial class Viewer: Entity
{
    public string Name { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string UserId { get; set; }

    public User User { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<FilmRating> FilmRatings { get; set; } = new List<FilmRating>();
}
