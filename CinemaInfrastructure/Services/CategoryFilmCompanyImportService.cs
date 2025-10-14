using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.EntityFrameworkCore;
using CinemaInfrastructure.Services;

public class CategoryFilmCompanyImportService : IImportService<Film>
{
    private readonly CinemaContext _context;

    public CategoryFilmCompanyImportService(CinemaContext context)
    {
        _context = context;
    }

    public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Неможливо читати потік", nameof(stream));

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        var errorLog = new List<string>(); // для помилок

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            try
            {
                string filmName = row.Cell(1).GetString().Trim();
                string categoryName = row.Cell(2).GetString().Trim();
                string companyName = row.Cell(3).GetString().Trim();
                string dateText = row.Cell(4).GetString().Trim();
                string description = row.Cell(5).GetString().Trim();
                string posterPath = row.Cell(6).GetString().Trim();

                if (string.IsNullOrEmpty(filmName)
                    || string.IsNullOrEmpty(categoryName)
                    || string.IsNullOrEmpty(companyName))
                {
                    errorLog.Add($"Рядок {row.RowNumber()}: Порожні обов'язкові поля.");
                    continue;
                }

                string dateOnlyText = dateText.Split(' ')[0];

                if (!DateTime.TryParseExact(dateOnlyText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var releaseDate))
                {
                    errorLog.Add($"Рядок {row.RowNumber()}: Некоректна дата {dateText}.");
                    continue;
                }

                var category = await _context.FilmCategories
                    .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken)
                    ?? new FilmCategory { Name = categoryName };

                try { ValidateOrThrow(category); }
                catch (ValidationException ve)
                {
                    errorLog.Add($"Рядок {row.RowNumber()}: Категорія — {ve.Message}");
                    continue;
                }

                if (category.Id == 0)
                    _context.FilmCategories.Add(category);

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name == companyName, cancellationToken)
                    ?? new Company { Name = companyName };

                try { ValidateOrThrow(company); }
                catch (ValidationException ve)
                {
                    errorLog.Add($"Рядок {row.RowNumber()}: Компанія — {ve.Message}");
                    continue;
                }

                if (company.Id == 0)
                    _context.Companies.Add(company);

                bool filmExists = await _context.Films
                    .AnyAsync(f => f.Name == filmName, cancellationToken);

                if (!filmExists)
                {
                    string posterRelativePath = "/img/empty_film_image.png";

                    if (!string.IsNullOrWhiteSpace(posterPath))
                    {
                        if (File.Exists(posterPath))
                        {
                            try
                            {
                                posterRelativePath = SavePoster(posterPath);
                            }
                            catch (Exception ex)
                            {
                                errorLog.Add($"Рядок {row.RowNumber()}: Помилка копіювання постера — {ex.Message}. Використано дефолтне зображення.");
                            }
                        }
                        else
                        {
                            errorLog.Add($"Рядок {row.RowNumber()}: Файл постера не знайдено — {posterPath}. Використано дефолтне зображення.");
                        }
                    }

                    var film = new Film
                    {
                        Name = filmName,
                        FilmCategory = category,
                        Company = company,
                        ReleaseDate = DateOnly.FromDateTime(releaseDate),
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        PosterPath = posterRelativePath
                    };

                    try { ValidateOrThrow(film); }
                    catch (ValidationException ve)
                    {
                        errorLog.Add($"Рядок {row.RowNumber()}: Фільм — {ve.Message}");
                        continue;
                    }

                    _context.Films.Add(film);
                }
                else
                {
                    // Логування дубліката і пропуск рядка
                    errorLog.Add($"Рядок {row.RowNumber()}: Фільм '{filmName}' вже існує. Пропускаємо.");
                }
            }
            catch (Exception ex)
            {
                errorLog.Add($"Рядок {row.RowNumber()}: Невідома помилка — {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (errorLog.Count > 0)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logPath = Path.Combine(desktopPath, "import_errors.log");
            await File.WriteAllLinesAsync(logPath, errorLog, cancellationToken);
        }
    }

    private static string SavePoster(string sourcePath)
    {
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string uploadsFolder = Path.Combine(wwwrootPath, "uploads");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
        string destPath = Path.Combine(uploadsFolder, uniqueFileName);
        File.Copy(sourcePath, destPath, overwrite: true);

        return "/uploads/" + uniqueFileName;
    }

    private static void ValidateOrThrow(object entity)
    {
        var ctx = new ValidationContext(entity);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(entity, ctx, errors, validateAllProperties: true))
        {
            throw new ValidationException(string.Join("; ", errors.Select(e => e.ErrorMessage)));
        }
    }
}
