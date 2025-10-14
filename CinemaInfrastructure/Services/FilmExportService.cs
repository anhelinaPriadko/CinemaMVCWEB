using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.EntityFrameworkCore;
using CinemaInfrastructure.Services;

namespace CinemaInfrastructure.Services
{
    public class FilmExportService : IExportService<Film>
    {
        private const string SheetName = "Films";
        private static readonly IReadOnlyList<string> HeaderNames = new[]
        {
            "Назва фільму",
            "Категорія",
            "Компанія",
            "Дата релізу",
            "Опис",
            "Постер"
        };

        private readonly CinemaContext _context;

        public FilmExportService(CinemaContext context)
        {
            _context = context;
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream is not writable", nameof(stream));

            var films = await _context.Films
                .Include(f => f.FilmCategory)
                .Include(f => f.Company)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(SheetName);

            // Header
            for (int i = 0; i < HeaderNames.Count; i++)
                worksheet.Cell(1, i + 1).Value = HeaderNames[i];
            worksheet.Row(1).Style.Font.Bold = true;

            // Data rows
            int row = 2;
            foreach (var film in films)
            {
                worksheet.Cell(row, 1).Value = film.Name;
                worksheet.Cell(row, 2).Value = film.FilmCategory?.Name;
                worksheet.Cell(row, 3).Value = film.Company?.Name;
                worksheet.Cell(row, 4).Value = film.ReleaseDate.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 5).Value = film.Description;
                worksheet.Cell(row, 6).Value = film.PosterPath;
                row++;
            }

            workbook.SaveAs(stream);
        }
    }
}