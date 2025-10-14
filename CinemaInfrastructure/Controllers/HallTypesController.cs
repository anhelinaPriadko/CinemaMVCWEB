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
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class HallTypesController : Controller
    {
        private readonly CinemaContext _context;

        public HallTypesController(CinemaContext context)
        {
            _context = context;
        }

        // GET: HallTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.HallTypes.ToListAsync());
        }

        // GET: HallTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hallType = await _context.HallTypes
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hallType == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByHallType", "Halls", new { hallTypeId = hallType.Id});
        }

        public bool CheckNameDublication(string Name)
        {
            return _context.HallTypes.Any(f => f.Name == Name);
        }

        // GET: HallTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: HallTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] HallType hallType)
        {
            if(CheckNameDublication(hallType.Name))
            {
                ModelState.AddModelError("Name", "Тип з такою назвою вже існує!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(hallType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hallType);
        }

        // GET: HallTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hallType = await _context.HallTypes.FindAsync(id);
            if (hallType == null)
            {
                return NotFound();
            }
            return View(hallType);
        }

        // POST: HallTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] HallType hallType)
        {
            if (id != hallType.Id)
            {
                return NotFound();
            }

            var existingHallType = await _context.HallTypes.FindAsync(id);
            if (existingHallType == null)
                return NotFound();

            if (existingHallType.Name != hallType.Name && CheckNameDublication(hallType.Name))
            {
                ModelState.AddModelError("Name", "Тип з такою назвою вже існує!");
            }

            if (!ModelState.IsValid)
            {
                return View(hallType);
            }

            try
            {
                existingHallType.Name = hallType.Name;
                _context.Update(existingHallType);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HallTypeExists(hallType.Id))
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

        // GET: HallTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hallType = await _context.HallTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hallType == null)
            {
                return NotFound();
            }

            return View(hallType);
        }

        // POST: HallTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hallType = await _context.HallTypes.FindAsync(id);
            if (hallType == null)
            {
                TempData["ErrorMessage"] = "Тип залу не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Halls.AnyAsync(h => h.HallTypeId == id);

            if (isLinked)
            {
                TempData["ErrorMessage"] = "Цей тип не можна видалити, оскільки він має пов'язані дані!";
                return RedirectToAction("Index");
            }

            _context.Remove(hallType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Тип \"{hallType.Name}\" успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        private bool HallTypeExists(int id)
        {
            return _context.HallTypes.Any(e => e.Id == id);
        }
    }
}
