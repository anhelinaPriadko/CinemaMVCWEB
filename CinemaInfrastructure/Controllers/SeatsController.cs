using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class SeatsController : Controller
    {
        private readonly CinemaContext _context;

        public SeatsController(CinemaContext context)
        {
            _context = context;
        }

        // GET: Seats
        public async Task<IActionResult> Index()
        {
            var cinemaContext = _context.Seats.Include(s => s.Hall);
            return View(await cinemaContext.ToListAsync());
        }

        public async Task<IActionResult> IndexByHall(int hallId)
        {
            var hallIdExists = await _context.Halls.AnyAsync(h => h.Id == hallId);
            if(!hallIdExists)
            {
                return NotFound();
            }

            var seats = await _context.Seats.Where(s => s.HallId == hallId)
                .ToListAsync();

            if(seats.Count() == 0)
                TempData["Message"] = "На жаль, в цьому залі ще немає місць(";

            return View("Index", seats);

        }

        // GET: Seats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (seat == null)
            {
                return NotFound();
            }

            return View(seat);
        }

        public async Task<IActionResult> DetailsByBookings(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats
                .Include(s => s.Hall)
                .Include(s => s.Hall.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (seat == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexBySeats", "Bookings", new { seatId = seat.Id});
        }

        // GET: Seats/Create
        public IActionResult Create()
        {
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name");
            return View();
        }

        private bool checkForDublications(int hallId, int rows, int numberInRow)
        {
            return _context.Seats
                .Any(s => s.HallId == hallId && s.Row == rows && s.NumberInRow == numberInRow);
        }

        // POST: Seats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HallId,Row,NumberInRow,Id")] Seat seat)
        {
            Hall hall = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(h => h.Id == seat.HallId);

            seat.Hall = hall;
            ModelState.Clear();
            TryValidateModel(seat);

            if(checkForDublications(seat.HallId, seat.Row, seat.NumberInRow))
            {
                ModelState.AddModelError("", "Таке місце в цьому залі вже існує!");
            }

            if(seat.Row > seat.Hall.NumberOfRows)
            {
                ModelState.AddModelError("Row", "Номер ряду не може перевищувати кількість рядів у залі!");
            }

            if (seat.NumberInRow > seat.Hall.SeatsInRow)
            {
                ModelState.AddModelError("NumberInRow", "Номер місця не може перевищувати кількість місць у ряду!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(seat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", seat.HallId);
            return View(seat);
        }

        //// GET: Seats/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var seat = await _context.Seats.FindAsync(id);
        //    if (seat == null)
        //    {
        //        return NotFound();
        //    }
        //    ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", seat.HallId);
        //    return View(seat);
        //}

        //// POST: Seats/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("HallId,Row,NumberInRow,Id")] Seat seat)
        //{
        //    if (id != seat.Id)
        //    {
        //        return NotFound();
        //    }

        //    Hall hall = await _context.Halls
        //        .Include(h => h.HallType)
        //        .FirstOrDefaultAsync(h => h.Id == seat.HallId);
        //    seat.Hall = hall;

        //    ModelState.Clear();
        //    TryValidateModel(seat);

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(seat);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!SeatExists(seat.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", seat.HallId);
        //    return View(seat);
        //}

        // GET: Seats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (seat == null)
            {
                return NotFound();
            }

            return View(seat);
        }

        // POST: Seats/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
            {
                TempData["ErrorMessage"] = "Місце не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Bookings.AnyAsync(b => b.SeatId == id);
            if(isLinked)
            {
                TempData["ErrorMessage"] = "Це місце не можна видалити, оскільки воно має пов'язані дані!";
                return RedirectToAction("Index");
            }

            _context.Remove(seat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Місце \"{seat.NumberInRow}\" ряду \"{seat.Row}\" успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        private bool SeatExists(int id)
        {
            return _context.Seats.Any(e => e.Id == id);
        }
    }
}
