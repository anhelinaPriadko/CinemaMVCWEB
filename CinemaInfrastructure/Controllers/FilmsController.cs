using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using CinemaInfrastructure.Services;

namespace CinemaInfrastructure.Controllers
{


    public class FilmsController : Controller
    {
        private readonly CinemaContext _context;
        private readonly IImportService<Film> _importService;
        private readonly IExportService<Film> _filmExportService;

        public FilmsController
            (CinemaContext context, IImportService<Film> importService, IExportService<Film> filmExportService)
        {
            _context = context;
            _importService = importService;
            _filmExportService = filmExportService;
        }

        // GET: /Films/Export
        [HttpGet]
        public IActionResult Export()
        {
            // Повертає форму з кнопкою
            return View();
        }

        // GET: /Films/ExportFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            await using var ms = new MemoryStream();
            await _filmExportService.WriteToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);

            string fileName = $"films_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            const string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            // Повертаємо файл клієнту
            return File(ms.ToArray(), contentType, fileName);
        }



        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile fileExcel)
        {
            if (fileExcel == null || fileExcel.Length == 0)
            {
                ModelState.AddModelError("", "Будь ласка, оберіть файл для імпорту.");
                return View();
            }

            using var stream = fileExcel.OpenReadStream();
            try
            {
                var importService = new CategoryFilmCompanyImportService(_context);
                await importService.ImportFromStreamAsync(stream);
                TempData["Success"] = "Імпорт завершено успішно.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Помилка під час імпорту: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        // GET: Films
        public async Task<IActionResult> Index()
        {
            var cinemaContext = _context.Films.Include(f => f.Company).Include(f => f.FilmCategory);
            return View(await cinemaContext.ToListAsync());
        }

        public async Task<IActionResult> IndexByCategory(int categoryId)
        {
            var categoryExists = await _context.FilmCategories.AnyAsync(c => c.Id == categoryId);
            if (!categoryExists)
            {
                return NotFound();
            }

            var films = await _context.Films
                .Where(f => f.FilmCategoryId == categoryId)
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .ToListAsync();

            if (films.Count == 0)
            {
                TempData["Message"] = "На жаль, фільмів за цією категорією ще немає(";
            }

            return View("Index", films);
        }

        public async Task<IActionResult> IndexByCompany(int companyId)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId);
            if (!companyExists)
            {
                return NotFound();
            }

            var films = await _context.Films
                .Where(f => f.CompanyId == companyId)
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .ToListAsync();

            if (films.Count == 0)
            {
                TempData["Message"] = "На жаль, фільмів фільмів від цієї кінокомпанії ще немає(";
            }

            return View("Index", films);
        }


        public bool CheckNameDublication(string Name)
        {
            return _context.Films.Any(f => f.Name == Name);
        }

        // GET: Films/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }

            return View(film);
        }

        public async Task<IActionResult> DetailsBySessions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByFilm", "Sessions", new { filmId = film.Id});
        }

        public async Task<IActionResult> DetailsByRatings(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByFilms", "FilmRatings", new { filmId = film.Id });
        }

        // GET: Films/Create
        public IActionResult Create(int? categoryId, int? companyId)
        {
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name");
            // Ви зберігаєте SelectList у ViewData["FilmCategoryId"]
            ViewData["FilmCategoryId"] = new SelectList(_context.FilmCategories, "Id", "Name");
            return View();
        }

        // POST: Films/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyId,FilmCategoryId,ReleaseDate,Description,Name,Id")] Film film, IFormFile? posterFile)
        {
            FilmCategory filmCategory = _context.FilmCategories
                .FirstOrDefault(f => f.Id == film.FilmCategoryId);

            Company company = _context.Companies
                .FirstOrDefault(c => c.Id == film.CompanyId);

            film.Company = company;
            film.FilmCategory = filmCategory;

            ModelState.Clear();
            TryValidateModel(film);

            if (CheckNameDublication(film.Name))
            {
                ModelState.AddModelError("Name", "Фільм з такою назвою вже існує!");
            }

            if (posterFile != null && posterFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }

                film.PosterPath = "/uploads/" + uniqueFileName;
            }
            else
                film.PosterPath = "/img/empty_film_image.png";

            if (ModelState.IsValid)
            {
                _context.Films.Add(film);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", film.CompanyId);
            ViewData["FilmCategoryId"] = new SelectList(_context.FilmCategories, "Id", "Name", film.FilmCategoryId);
            return View(film);
        }

        // GET: Films/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films.FindAsync(id);
            if (film == null)
            {
                return NotFound();
            }
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", film.CompanyId);
            ViewData["FilmCategoryId"] = new SelectList(_context.FilmCategories, "Id", "Name", film.FilmCategoryId);
            return View(film);
        }

        // POST: Films/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CompanyId,FilmCategoryId,ReleaseDate,Description,Name,Id")] Film film, IFormFile? posterFile)
        {
            if (id != film.Id)
            {
                return NotFound();
            }

            FilmCategory filmCategory = await _context.FilmCategories
                .FindAsync(film.FilmCategoryId);
            Company company = await _context.Companies
                .FindAsync(film.CompanyId);

            film.FilmCategory = filmCategory;
            film.Company = company;

            film.Company = company;
            film.FilmCategory = filmCategory;
            ModelState.Clear();
            TryValidateModel(film);

            var existingFilm = await _context.Films.FindAsync(id);
            if (existingFilm == null)
                return NotFound();

            if (existingFilm.Name != film.Name && CheckNameDublication(film.Name))
            {
                ModelState.AddModelError("Name", "Фільм з такою назвою вже існує!");
            }

            if (posterFile != null && posterFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }

                film.PosterPath = "/uploads/" + uniqueFileName;
            }
            else
                film.PosterPath = "/img/empty_film_image.png";

            if (ModelState.IsValid)
            {
                try
                {
                    existingFilm.Name = film.Name;
                    existingFilm.CompanyId = film.CompanyId;
                    existingFilm.Company = film.Company;
                    existingFilm.FilmCategoryId = film.FilmCategoryId;
                    existingFilm.FilmCategory = film.FilmCategory;
                    existingFilm.PosterPath = film.PosterPath;
                    _context.Update(existingFilm);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FilmExists(film.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", film.CompanyId);
            ViewData["FilmCategoryId"] = new SelectList(_context.FilmCategories, "Id", "Name", film.FilmCategoryId);
            return View(film);
        }


        // GET: Films/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }

            return View(film);
        }

        // POST: Films/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var film = await _context.Films.FindAsync(id);
            if (film == null)
            {
                TempData["ErrorMessage"] = "Фільм не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Sessions.AnyAsync(s => s.FilmId == id);

            if(isLinked)
            {
                TempData["ErrorMessage"] = "Цей фільм не можна видалити, оскільки він має пов'язані дані!";
                return RedirectToAction("Index");
            }

            await _context.FilmRatings.Where(f => f.FilmId == id).ExecuteDeleteAsync();

            _context.Remove(film);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Фільм \"{film.Name}\" успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        private bool FilmExists(int id)
        {
            return _context.Films.Any(e => e.Id == id);
        }
    }
}
