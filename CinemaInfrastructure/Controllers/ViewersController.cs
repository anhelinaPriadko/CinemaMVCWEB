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
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    //[Authorize(Roles = "superadmin, user")]
    public class ViewersController : Controller
    {
        private readonly CinemaContext _context;

        public ViewersController(CinemaContext context)
        {
            _context = context;
        }

        // GET: Viewers
        public async Task<IActionResult> Index()
        {
            var viewers = await _context.Viewers
                .Include(v => v.User) // підвантажуємо зв'язану таблицю
                .ToListAsync();

            return View(viewers);
        }

        // GET: Viewers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewer = await _context.Viewers
                .Include(v => v.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (viewer == null)
            {
                return NotFound();
            }

            return View(viewer);
        }

        public async Task<IActionResult> DetailsByRatings(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewer = await _context.Viewers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (viewer == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByViewers", "FilmRatings", new { viewerId = viewer.Id });
        }

        public async Task<IActionResult> DetailsByBookings(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewer = await _context.Viewers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (viewer == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByViewers", "Bookings", new { viewerId = viewer.Id });
        }

        private bool checkDuplication(string viewerName)
        {
            return _context.Viewers.Any(v => v.Name == viewerName);
        }

        private bool checkAge(DateOnly viewerAge, int minAge)
        {
            DateOnly theYoungestAge = DateOnly.FromDateTime(DateTime.Now);
            DateOnly result = theYoungestAge.AddYears(-minAge);

            return viewerAge < result;
        }

        // GET: Viewers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Viewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DateOfBirth,Id")] Viewer viewer)
        {
            if (checkDuplication(viewer.Name))
                ModelState.AddModelError("Name", "Користувач з таким ім'ям вже існує!");

            if (!checkAge(viewer.DateOfBirth, 14))
                ModelState.AddModelError("DateOfBirth", "Мінімальний вік користувача має бути 14 років!");

            if (ModelState.IsValid)
            {
                _context.Add(viewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(viewer);
        }

        // GET: Viewers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewer = await _context.Viewers.Include(v => v.User)
                                               .FirstOrDefaultAsync(v => v.Id == id);
            if (viewer == null)
            {
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", viewer.UserId);

            return View(viewer);
        }

        // POST: Viewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] Viewer viewer)
        {
            if (id != viewer.Id)
            {
                return NotFound();
            }

            var existingViewer = await _context.Viewers.FindAsync(id);
            if (existingViewer == null)
            {
                return NotFound();
            }

            var existingUser = await _context.Users.FindAsync(existingViewer.UserId);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingViewer.User = existingUser;

            ModelState.Clear();
            TryValidateModel(existingViewer);

            if (ModelState.IsValid)
            {
                try
                {
                    existingViewer.Name = viewer.Name;
                    viewer.User = existingUser;
                    viewer.DateOfBirth = existingViewer.DateOfBirth;
                    _context.Update(existingViewer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ViewerExists(viewer.Id))
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

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", viewer.UserId);
            return View(viewer);
        }

        // GET: Viewers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewer = await _context.Viewers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (viewer == null)
            {
                return NotFound();
            }

            return View(viewer);
        }

        // POST: Viewers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var viewer = await _context.Viewers.FindAsync(id);
            if (viewer != null)
            {
                _context.Viewers.Remove(viewer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ViewerExists(int id)
        {
            return _context.Viewers.Any(e => e.Id == id);
        }
    }
}
