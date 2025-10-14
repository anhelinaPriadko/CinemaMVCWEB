using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CinemaInfrastructure.Controllers
{
    public class FilmRatingsController : Controller
    {
        private readonly CinemaContext _context;

        public FilmRatingsController(CinemaContext context)
        {
            _context = context;
        }

        // GET: FilmRatings
        public async Task<IActionResult> Index()
        {
            var cinemaContext = _context.FilmRatings.Include(f => f.Film).Include(f => f.Viewer);
            return View(await cinemaContext.ToListAsync());
        }

        public async Task<IActionResult> IndexByFilms(int filmId)
        {
            var seatExists = await _context.Seats.AnyAsync(s => s.Id == filmId);
            if (!seatExists)
                return NotFound();

            var filmRatings = await _context.FilmRatings
                .Where(f => f.FilmId == filmId)
                .Include(f => f.Film)
                .Include(f => f.Viewer)
                .ToArrayAsync();

            if(filmRatings.Count() == 0)
                TempData["Message"] = "На жаль, оцінок для цього фільму ще немає(";

            return View("Index", filmRatings);
        }

        public async Task<IActionResult> IndexByViewers(int viewerId)
        {
            var viewerExists = await _context.Viewers.AnyAsync(s => s.Id == viewerId);
            if (!viewerExists)
                return NotFound();

            var filmRatings = await _context.FilmRatings
                .Where(f => f.ViewerId == viewerId)
                .Include(f => f.Film)
                .Include(f => f.Viewer)
                .ToArrayAsync();

            if (filmRatings.Count() == 0)
                TempData["Message"] = "На жаль, цйе користувач ще не залишав оцшнок(";

            return View("Index", filmRatings);
        }

        // GET: FilmRatings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmRating = await _context.FilmRatings
                .Include(f => f.Film)
                .Include(f => f.Viewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (filmRating == null)
            {
                return NotFound();
            }

            return View(filmRating);
        }

        private bool checkDublications(int viewerId, int filmId)
        {
            return _context.FilmRatings.Any(f => f.ViewerId == viewerId && f.FilmId == filmId);
        }

        // GET: FilmRatings/Create
        public IActionResult Create()
        {
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name");
            ViewData["ViewerId"] = new SelectList(_context.Viewers, "Id", "Name");
            return View();
        }

        // POST: FilmRatings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ViewerId,FilmId,Rating,Id")] FilmRating filmRating)
        {
            Film film = _context.Films.Include(f => f.FilmCategory)
                .Include(f => f.Company)
                .FirstOrDefault(f => f.Id == filmRating.FilmId);

            Viewer viewer = _context.Viewers
                .FirstOrDefault(v => v.Id == filmRating.ViewerId);

            filmRating.Film = film;
            filmRating.Viewer = viewer;

            ModelState.Clear();
            TryValidateModel(filmRating);

            if(checkDublications(filmRating.ViewerId, filmRating.FilmId))
            {
                ModelState.AddModelError("", "Даний користувач вже залишав оцінку для цього фільму!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(filmRating);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", filmRating.FilmId);
            ViewData["ViewerId"] = new SelectList(_context.Viewers, "Id", "Name", filmRating.ViewerId);
            return View(filmRating);
        }

        // GET: FilmRatings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmRating = await _context.FilmRatings.FindAsync(id);
            if (filmRating == null)
            {
                return NotFound();
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", filmRating.FilmId);
            ViewData["ViewerId"] = new SelectList(_context.Viewers, "Id", "Name", filmRating.ViewerId);
            return View(filmRating);
        }

        // POST: FilmRatings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ViewerId,FilmId,Rating,Id")] FilmRating filmRating)
        {
            if (id != filmRating.Id)
            {
                return NotFound();
            }

            Viewer viewer = await _context.Viewers.FindAsync(filmRating.ViewerId);

            Film film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(f => f.Id == filmRating.FilmId);

            if (film == null || viewer == null)
            {
                return NotFound();
            }

            filmRating.Viewer = viewer;
            filmRating.Film = film;

            ModelState.Clear();
            TryValidateModel(filmRating);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(filmRating);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FilmRatingExists(filmRating.Id))
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
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", filmRating.FilmId);
            ViewData["ViewerId"] = new SelectList(_context.Viewers, "Id", "Name", filmRating.ViewerId);
            return View(filmRating);
        }

        // GET: FilmRatings/Delete/5
        public async Task<IActionResult> Delete(int filmId, int viewerId)
        {
            var filmRating = await _context.FilmRatings
                .Include(f => f.Film)
                .Include(f => f.Viewer)
                .FirstOrDefaultAsync(m => m.FilmId == filmId && m.ViewerId == viewerId);

            if (filmRating == null)
            {
                return NotFound();
            }

            return View(filmRating);
        }

        // POST: FilmRatings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int filmId, int viewerId)
        {
            var filmRating = await _context.FilmRatings
                .Include(f => f.Viewer)
                .Include(f => f.Film)
                .FirstOrDefaultAsync(fr => fr.FilmId == filmId && fr.ViewerId == viewerId);

            if (filmRating != null)
            {
                _context.FilmRatings.Remove(filmRating);
                TempData["SuccessMessage"] = $"Оцінку користувача \"{filmRating.Viewer.Name}\" для фільму \"{filmRating.Film.Name}\" успішно видалено!";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FilmRatingExists(int id)
        {
            return _context.FilmRatings.Any(e => e.Id == id);
        }
    }
}
