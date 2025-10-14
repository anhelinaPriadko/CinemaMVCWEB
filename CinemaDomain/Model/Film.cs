using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Film : Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [StringLength(40, ErrorMessage = "Назва фільму не може перевищувати 40 символів!")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    public int FilmCategoryId { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [FutureDate("1896-01-25")]
    [MaxFutureDate(3)]
    public DateOnly ReleaseDate { get; set; }

    public string? Description { get; set; }
    
    public string? PosterPath { get; set; }  

    public virtual Company Company { get; set; } = null!;

    public virtual FilmCategory FilmCategory { get; set; } = null!;

    public virtual ICollection<FilmRating> FilmRatings { get; set; } = new List<FilmRating>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public class FutureDateAttribute : ValidationAttribute
{
    private readonly DateOnly _minDate;

    public FutureDateAttribute(string minDate)
    {
        _minDate = DateOnly.Parse(minDate);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly dateValue && dateValue <= _minDate)
        {
            return new ValidationResult($"Дата повинна бути пізнішою за {_minDate:dd.MM.yyyy}!");
        }
        return ValidationResult.Success;
    }
}

public class MaxFutureDateAttribute : ValidationAttribute
{
    private readonly int _mounthToAdd;

    public MaxFutureDateAttribute(int mounthToAdd)
    {
        _mounthToAdd = mounthToAdd;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateOnly dateTimeValue)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            var MaxAllowedDate = today.AddMonths(_mounthToAdd);
            if (dateTimeValue > MaxAllowedDate)
            {
                return new ValidationResult(
                    $"Дата випуску фільму має бути не пізніше ніж {MaxAllowedDate:dd.MM.yyyy}!"
                );
            }
        }
        return ValidationResult.Success;
    }
}
