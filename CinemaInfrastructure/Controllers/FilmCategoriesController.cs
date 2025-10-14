using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.VisualBasic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class FilmCategoriesController : Controller
    {
        private readonly CinemaContext _context;

        public FilmCategoriesController(CinemaContext context)
        {
            _context = context;
        }

        // GET: FilmCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.FilmCategories.ToListAsync());
        }

        // GET: FilmCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmCategory = await _context.FilmCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (filmCategory == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByCategory", "Films", new { categoryId = filmCategory.Id });
        }

        public bool CheckNameDublication(string Name)
        {
            return _context.FilmCategories.Any(f => f.Name == Name);
        }

        // GET: FilmCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FilmCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] FilmCategory filmCategory)
        {
            if (CheckNameDublication(filmCategory.Name))
            {
                ModelState.AddModelError("Name", "Категорія з такою назвою вже існує!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(filmCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(filmCategory);
        }

        // GET: FilmCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmCategory = await _context.FilmCategories.FindAsync(id);
            if (filmCategory == null)
            {
                return NotFound();
            }
            return View(filmCategory);
        }

        // POST: FilmCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] FilmCategory filmCategory)
        {
            if (id != filmCategory.Id)
            {
                return NotFound();
            }

            var existingFilmCategory = await _context.FilmCategories.FindAsync(id);
            if (existingFilmCategory == null)
                return NotFound();

            if (existingFilmCategory.Name != filmCategory.Name && CheckNameDublication(filmCategory.Name))
            {
                ModelState.AddModelError("Name", "Категорія з такою назвою вже існує!");
            }

            if (!ModelState.IsValid)
            {
                return View(filmCategory);
            }

            try
            {
                existingFilmCategory.Name = filmCategory.Name;
                _context.Update(existingFilmCategory);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FilmCategoryExists(filmCategory.Id))
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

        // GET: FilmCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmCategory = await _context.FilmCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (filmCategory == null)
            {
                return NotFound();
            }

            return View(filmCategory);
        }

        // POST: FilmCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var filmCategory = await _context.FilmCategories.FindAsync(id);
            if (filmCategory == null)
            {
                TempData["ErrorMessage"] = "Категорію не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Films.AnyAsync(f => f.FilmCategoryId == id);

            if (isLinked)
            {
                TempData["ErrorMessage"] = "Цю категорію не можна видалити, оскільки вона має пов'язані дані!";
                return RedirectToAction("Index");
            }

            _context.Remove(filmCategory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Категорію \"{filmCategory.Name}\" успішно видалено!";
            return RedirectToAction("Index");
        }

        private bool FilmCategoryExists(int id)
        {
            return _context.FilmCategories.Any(e => e.Id == id);
        }
    }
}
