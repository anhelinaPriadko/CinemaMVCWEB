using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class HallsController : Controller
    {
        private readonly CinemaContext _context;

        public HallsController(CinemaContext context)
        {
            _context = context;
        }

        // GET: Halls
        public async Task<IActionResult> Index()
        {
            var cinemaContext = _context.Halls.Include(h => h.HallType);
            return View(await cinemaContext.ToListAsync());
        }

        public async Task<IActionResult> IndexByHallType(int hallTypeId)
        {
            var hallTypeExists = await _context.HallTypes.AnyAsync(h => h.Id == hallTypeId);
            if(!hallTypeExists)
            {
                return NotFound();
            }

            var halls = await _context.Halls
                .Where(h => h.HallTypeId == hallTypeId)
                .ToListAsync();

            if(halls.Count() == 0)
                TempData["Message"] = "На жаль, залів цього типу ще немає(";

            return View("Index", halls);
        }

        // GET: Halls/Details/5
        public async Task<IActionResult> DetailsBySeats(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByHall", "Seats", new { hallId = hall.Id});
        }

        public async Task<IActionResult> DetailsBySessions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByHall", "Sessions", new { hallId = hall.Id });
        }

        public bool CheckNameDublication(string Name)
        {
            return _context.Halls.Any(h => h.Name == Name);
        }

        private string? AddOrUpdateSeats(Hall hall, int newNumberOfRows, int newSeatsInRow)
        {
            // Завантажуємо місця
            var oldSeats = hall.Seats.ToList();
            var bookedSeats = oldSeats
                .Where(s => _context.Bookings.Any(b => b.SeatId == s.Id))
                .ToList();

            // Перевірка зменшення
            if (newNumberOfRows < hall.NumberOfRows || newSeatsInRow < hall.SeatsInRow)
            {
                var seatsToBeRemoved = oldSeats
                    .Where(s => s.Row > newNumberOfRows || s.NumberInRow > newSeatsInRow)
                    .ToList();

                // Якщо серед них є заброньовані, повертаємо помилку
                if (seatsToBeRemoved.Any(s => bookedSeats.Contains(s)))
                {
                    return "Неможливо зменшити кількість рядів або місць у ряду, оскільки деякі місця вже заброньовані!";
                }

                // Видаляємо ті, що не заброньовані
                _context.Seats.RemoveRange(seatsToBeRemoved.Except(bookedSeats));
            }

            // Додаємо нові місця
            for (int i = 1; i <= newNumberOfRows; i++)
            {
                for (int j = 1; j <= newSeatsInRow; j++)
                {
                    if (!oldSeats.Any(s => s.Row == i && s.NumberInRow == j))
                    {
                        _context.Seats.Add(new Seat { HallId = hall.Id, Row = i, NumberInRow = j });
                    }
                }
            }

            // Оновлюємо кількість рядів і місць
            hall.NumberOfRows = newNumberOfRows;
            hall.SeatsInRow = newSeatsInRow;

            return null; // помилки немає
        }

        // GET: Halls/Create
        public IActionResult Create()
        {
            ViewData["HallTypeId"] = new SelectList(_context.HallTypes, "Id", "Name");
            return View();
        }

        // POST: Halls/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,NumberOfRows,SeatsInRow,HallTypeId,Id")] Hall hall)
        {
            HallType hallType = _context.HallTypes.Include(h => h.Halls)
                .FirstOrDefault(h => h.Id == hall.HallTypeId);

            hall.HallType = hallType;
            ModelState.Clear();
            TryValidateModel(hall);

            if(CheckNameDublication(hall.Name))
            {
                ModelState.AddModelError("Name", "Зал з такою назвою вже існує!");
            }

            if (!ModelState.IsValid)
            {
                ViewData["HallTypeId"] = new SelectList(_context.HallTypes, "Id", "Name", hall.HallTypeId);
                return View(hall);
            }

            hall.Seats = new List<Seat>();

            // 4. Додаємо потрібну кількість місць
            for (int row = 1; row <= hall.NumberOfRows; row++)
            {
                for (int seatNum = 1; seatNum <= hall.SeatsInRow; seatNum++)
                {
                    hall.Seats.Add(new Seat
                    {
                        Row = row,
                        NumberInRow = seatNum
                    });
                }
            }

            // 5. Зберігаємо Зал разом із місцями (EF Core створить і Зал, і Seats одночасно)
            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Halls/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls.FindAsync(id);

            if (hall == null)
            {
                return NotFound();
            }

            ViewData["HallTypeId"] = new SelectList(_context.HallTypes, "Id", "Name", hall.HallTypeId);
            return View(hall);
        }

        // POST: Halls/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,NumberOfRows,SeatsInRow,HallTypeId,Id")] Hall hall)
        {
            if (id != hall.Id) return NotFound();

            var existingHall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == id);
            HallType hallType = await _context.HallTypes.FirstOrDefaultAsync(h => h.Id == hall.HallTypeId);
            hall.HallType = hallType;

            ModelState.Clear();
            TryValidateModel(hall);

            if (existingHall == null) return NotFound();

            // Перевірка дублювання імені
            if (existingHall.Name != hall.Name && CheckNameDublication(hall.Name))
            {
                ModelState.AddModelError("Name", "Зал з такою назвою вже існує!");
            }

            if (!ModelState.IsValid)
            {
                ViewData["HallTypeId"] = new SelectList(_context.HallTypes, "Id", "Name", hall.HallTypeId);
                return View(hall);
            }

            existingHall.Name = hall.Name;
            existingHall.HallTypeId = hall.HallTypeId;
            existingHall.HallType = hallType;

            var error = AddOrUpdateSeats(existingHall, hall.NumberOfRows, hall.SeatsInRow);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                if (!ModelState.IsValid)
                {
                    ViewData["HallTypeId"] = new SelectList(_context.HallTypes, "Id", "Name", hall.HallTypeId);
                    return View(hall);
                }
            }

            // Зберігаємо
            try
            {
                _context.Update(existingHall);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HallExists(hall.Id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }



        // GET: Halls/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null)
            {
                return NotFound();
            }

            return View(hall);
        }

        // POST: Halls/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                TempData["ErrorMessage"] = "Зал не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinkedSeats = await _context.Seats.AnyAsync(s => s.HallId == id);
            var isLinkedSessions = await _context.Sessions.AnyAsync(s => s.HallId == id);

            if (isLinkedSeats || isLinkedSessions)
            {
                TempData["ErrorMessage"] = "Цей зал не можна видалити, оскільки він має пов'язані дані!";
                return RedirectToAction("Index");
            }

            _context.Remove(hall);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Зал \"{hall.Name}\" успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        private bool HallExists(int id)
        {
            return _context.Halls.Any(e => e.Id == id);
        }
    }
}
