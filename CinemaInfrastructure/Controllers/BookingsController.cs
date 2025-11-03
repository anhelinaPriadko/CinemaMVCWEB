using CinemaDomain.Model;
using CinemaInfrastructure;
using CinemaInfrastructure.Models;
using CinemaInfrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers
{
    public class BookingsController : Controller
    {
        private readonly CinemaContext _context;
        private readonly UserManager<User> _userManager;
        private readonly BookingService _bookingService;

        public BookingsController(CinemaContext context, UserManager<User> userManager, BookingService bookingService)
        {
            _context = context;
            _userManager = userManager;
            _bookingService = bookingService;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            IQueryable<Booking> bookingsQuery = _context.Bookings; // Починаємо без Include

            if (User.IsInRole("user"))
            {
                var currentUserId = _userManager.GetUserId(User);
                bookingsQuery = bookingsQuery.Where(b => b.Viewer.UserId == currentUserId);
            }

            // ПРОЕКЦІЯ (SELECT): Отримуємо лише потрібні дані одним запитом (JOIN)
            var bookings = await bookingsQuery
                .OrderByDescending(b => b.Session.SessionTime)
                .Select(b => new BookingIndexViewModel
                {
                    ViewerId = b.ViewerId,
                    SessionId = b.SessionId,
                    SeatId = b.SeatId,
                    FilmName = b.Session.Film.Name,
                    SessionTime = b.Session.SessionTime,
                    SeatRow = b.Seat.Row,
                    SeatNumberInRow = b.Seat.NumberInRow,
                    ViewerName = b.Viewer.Name
                })
                .ToListAsync();

            // ⚠️ Змінено: View тепер приймає ViewModel
            return View(bookings);
        }

        public async Task<IActionResult> IndexBySessions(int sessionId)
        {
            var sessionExists = await _context.Sessions.AnyAsync(s => s.Id == sessionId);
            if (!sessionExists)
                return NotFound();

            var bookings = await _context.Bookings.Where(b => b.SessionId == sessionId)
                .Include(b => b.Seat)
                    .ThenInclude(s => s.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Viewer)
                .OrderByDescending(b => b.Session.SessionTime)
                .ToListAsync();

            return View("Index", bookings);
        }

        public async Task<IActionResult> IndexByViewers(int viewerId)
        {
            var viewerExists = await _context.Viewers.AnyAsync(s => s.Id == viewerId);
            if (!viewerExists)
                return NotFound();

            var bookings = await _context.Bookings.Where(b => b.ViewerId == viewerId)
                .Include(b => b.Seat)
                    .ThenInclude(s => s.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Viewer)
                .OrderByDescending(b => b.Session.SessionTime)
                .ToListAsync();

            if (bookings.Count() == 0)
                TempData["Message"] = "На жаль, у цього користувача ще немає бронювань(";

            return View("Index", bookings);
        }

        public async Task<IActionResult> IndexBySeats(int seatId)
        {
            var seatExists = await _context.Seats.AnyAsync(s => s.Id == seatId);
            if (!seatExists)
                return NotFound();

            var bookings = await _context.Bookings.Where(b => b.SeatId == seatId)
                .Include(b => b.Seat)
                    .ThenInclude(s => s.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Viewer)
                .ToListAsync();


            return View("Index", bookings);
        }

        /// <summary>
        /// Ендпоінт для Adaptive Polling. Повертає список всіх бронювань та їх загальну кількість.
        /// Клієнт буде використовувати цю загальну кількість для визначення змін.
        /// </summary>
        public async Task<IActionResult> GetRecentUpdates()
        {
            var allBookings = await _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                .Include(b => b.Seat)
                .Include(b => b.Viewer)
                .ToListAsync();

            // Перетворюємо у ViewModel для безпечної передачі
            var updates = allBookings
                .Select(b => new
                {
                    ViewerId = b.ViewerId,
                    SessionId = b.SessionId,
                    SeatId = b.SeatId,
                    FilmName = b.Session?.Film?.Name ?? "N/A",
                    SeatInfo = b.Seat != null ? $"Ряд {b.Seat.Row}, Місце {b.Seat.NumberInRow}" : "N/A",
                    ViewerName = b.Viewer?.Name ?? "Анонім",
                    SessionTime = b.Session.SessionTime.ToString("dd.MM.yyyy HH:mm")
                })
                .ToList();

            return Json(new { Count = updates.Count, Updates = updates, LastUpdate = DateTime.UtcNow });
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? viewerId, int? sessionId, int? seatId)
        {
            if (viewerId == null || sessionId == null || seatId == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Seat)
                    .ThenInclude(s => s.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Viewer)
                .FirstOrDefaultAsync(m =>
                    m.ViewerId == viewerId &&
                    m.SessionId == sessionId &&
                    m.SeatId == seatId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        public async Task<IActionResult> GetSeatMap(int sessionId)
        {
            var session = await _context.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                return NotFound("Сеанс не знайдено.");

            int hallId = session.HallId;

            var seats = await _context.Seats
                .Where(s => s.HallId == hallId)
                .Select(s => new
                {
                    s.Id,
                    s.Row,
                    s.NumberInRow,
                    IsBooked = _context.Bookings.Any(b => b.SeatId == s.Id && b.SessionId == sessionId)
                }).ToListAsync();

            return Json(seats);
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create(int? FilmId, int? sessionId)
        {
            // 1) Фільми
            var films = await _context.Films.ToListAsync();
            ViewBag.Films = films;
            ViewBag.SelectedFilmId = FilmId;
            ViewBag.SelectedSessionId = sessionId;

            // 2) Глядачі — різні для user / admin
            if (User.IsInRole("user"))
            {
                var currentUserId = _userManager.GetUserId(User);
                var myViewer = await _context.Viewers
                    .Where(v => v.UserId == currentUserId)
                    .ToListAsync();
                ViewData["ViewerId"] = new SelectList(myViewer, "Id", "Name", myViewer.First().Id);
                ViewBag.IsUser = true;
            }
            else
            {
                var allViewers = await _context.Viewers.ToListAsync();
                ViewData["ViewerId"] = new SelectList(allViewers, "Id", "Name");
                ViewBag.IsUser = false;
            }

            return View();
        }


        public async Task<IActionResult> GetSessionsByFilm(int filmId)
        {
            var now = DateTime.Now;

            var sessions = await _context.Sessions
                .Where(s => s.FilmId == filmId
                            && s.SessionTime > now)
                .OrderBy(s => s.SessionTime)
                .Select(s => new {
                    id = s.Id,
                    time = s.SessionTime.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();
            return Json(sessions);
        }

        public async Task<IActionResult> GetRowsBySession(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return Json(new List<object>());

            var rows = await _context.Seats
                .Where(s => s.Hall.Id == session.Hall.Id)
                .Select(s => s.Row)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
            return Json(rows);
        }


        public async Task<IActionResult> GetSeatsByRow(int sessionId, int row)
        {
            var session = await _context.Sessions
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                return Json(new List<object>());

            var seats = await _context.Seats
                .Where(s => s.Hall.Id == session.Hall.Id && s.Row == row)
                .Select(s => new {
                    s.Id,
                    s.NumberInRow
                })
                .OrderBy(s => s.NumberInRow)
                .ToListAsync();
            return Json(seats);
        }

        private bool checkDublication(int sessioId, int seatId)
        {
            return _context.Bookings.Any(b => b.SessionId == sessioId && b.SeatId == seatId);
        }


        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ViewerId,SessionId,SeatId")] Booking booking)
        {
            booking.Viewer = await _context.Viewers
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == booking.ViewerId);
            booking.Session = await _context.Sessions
                .Include(s => s.Hall)
                .Include(s => s.Film)
                .Include(s => s.Film.FilmCategory)
                .Include(s => s.Film.Company)
                .FirstOrDefaultAsync(s => s.Id == booking.SessionId);
            booking.Seat = await _context.Seats
                .Include(s => s.Hall)
                .Include(s => s.Hall.HallType)
                .FirstOrDefaultAsync(s => s.Id == booking.SeatId);

            ModelState.Clear();
            TryValidateModel(booking);

            if (checkDublication(booking.SessionId, booking.SeatId))
            {
                ModelState.AddModelError("", "Дане місце на цей сеанс вже заброньовано!");
            }

            var films = _context.Films.ToList();
            var viewers = _context.Viewers.ToList();

            if (!ModelState.IsValid)
            {
                ViewBag.Films = films;
                ViewData["ViewerId"] = new SelectList(viewers, "Id", "Name", booking.ViewerId);
                return View(booking);
            }

            try
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();

                var bookingForNotification = await _context.Bookings
                    .Include(b => b.Session).ThenInclude(s => s.Film)
                    .Include(b => b.Seat)
                    .Include(b => b.Viewer)
                    .FirstOrDefaultAsync(b => b.ViewerId == booking.ViewerId
                                           && b.SessionId == booking.SessionId
                                           && b.SeatId == booking.SeatId);

                if (bookingForNotification != null)
                {
                    await _bookingService.SendBookingNotificationAsync(bookingForNotification, "Created");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Помилка бази даних: Не вдалося створити бронювання.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Виникла несподівана помилка.");
            }

            ViewBag.Films = films;
            ViewData["ViewerId"] = new SelectList(viewers, "Id", "Name", booking.ViewerId);
            return View(booking);
        }


        public async Task<IActionResult> Edit(int viewerId, int sessionId, int seatId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Seat)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                .FirstOrDefaultAsync(b =>
                    b.ViewerId == viewerId &&
                    b.SessionId == sessionId &&
                    b.SeatId == seatId);

            if (booking == null)
                return NotFound();

            ViewBag.Films = _context.Films.ToList();

            // Формуємо SelectList для сеансів того самого фільму
            var sessions = _context.Sessions
                .Where(s => s.FilmId == booking.Session.FilmId)
                .Select(s => new {
                    s.Id,
                    Time = s.SessionTime.ToString("dd.MM.yyyy HH:mm")
                }).ToList();
            ViewBag.SessionTime = new SelectList(sessions, "Id", "Time", booking.SessionId);

            return View(booking);
        }


        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int viewerId,
            int sessionId,    // початковий сеанс
            int seatId,       // початкове місце
            int newSessionId, // новий сеанс (якщо змінюємо)
            int newSeatId)    // нове місце
        {
            // Знаходимо існуюче бронювання
            var oldBooking = await _context.Bookings.FindAsync(viewerId, sessionId, seatId);
            if (oldBooking == null)
                return NotFound();

            // Перевірка дублювання бронювання (чи зайняте вже нове місце)
            if (_context.Bookings.Any(b => b.SessionId == newSessionId && b.SeatId == newSeatId))
            {
                ModelState.AddModelError("", "Обране місце на цей сеанс вже заброньовано!");
            }

            if (!ModelState.IsValid)
            {
                // Якщо валідація не пройшла, відновлюємо дані для відображення форми
                var sessions = await _context.Sessions
                    .Where(s => s.FilmId == oldBooking.Session.FilmId)
                    .Select(s => new {
                        s.Id,
                        Time = s.SessionTime.ToString("dd.MM.yyyy HH:mm")
                    })
                    .ToListAsync();
                ViewBag.SessionTime = new SelectList(sessions, "Id", "Time", newSessionId);

                // Можна також повторно завантажити ряд та місця (опціонально)
                return View(oldBooking);
            }

            // Оскільки ключі бронювання – композиційні, змінювати їх напряму складно.
            // Тому видаляємо старе бронювання та додаємо нове.
            _context.Bookings.Remove(oldBooking);
            var newBooking = new Booking
            {
                ViewerId = oldBooking.ViewerId,
                SessionId = newSessionId,
                SeatId = newSeatId
            };

            // Завантаження пов'язаних даних (якщо потрібно для подальшої роботи)
            newBooking.Viewer = await _context.Viewers.FirstOrDefaultAsync(v => v.Id == newBooking.ViewerId);
            newBooking.Session = await _context.Sessions
                .Include(s => s.Film)
                .FirstOrDefaultAsync(s => s.Id == newBooking.SessionId);
            newBooking.Seat = await _context.Seats
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == newBooking.SeatId);

            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();

            var newBookingForNotification = await _context.Bookings
            .Include(b => b.Session).ThenInclude(s => s.Film)
            .Include(b => b.Seat)
            .Include(b => b.Viewer)
            .FirstOrDefaultAsync(b => b.ViewerId == newBooking.ViewerId
                                   && b.SessionId == newBooking.SessionId
                                   && b.SeatId == newBooking.SeatId);

            if (newBookingForNotification != null)
            {
                await _bookingService.SendBookingNotificationAsync(oldBooking, "Deleted");
                await _bookingService.SendBookingNotificationAsync(newBookingForNotification, "Updated");
            }

            return RedirectToAction(nameof(Index));
        }



        private bool BookingExists(int viewerId, int sessionId, int seatId)
        {
            return _context.Bookings.Any(e =>
                e.ViewerId == viewerId &&
                e.SessionId == sessionId &&
                e.SeatId == seatId
            );
        }


        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int viewerId, int sessionId, int seatId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Seat)
                .Include(b => b.Session)
                .Include(b => b.Viewer)
                .FirstOrDefaultAsync(b =>
                    b.ViewerId == viewerId &&
                    b.SessionId == sessionId &&
                    b.SeatId == seatId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int viewerId, int sessionId, int seatId)
        {
            var booking = await _context.Bookings
                    .Include(b => b.Viewer)
                    .Include(b => b.Session)
                    .Include(b => b.Session.Film)
                    .FirstOrDefaultAsync(b => b.ViewerId == viewerId &&
                    b.SessionId == sessionId && b.SeatId == seatId);

            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Бронювання користувача \"{booking.Viewer.Name}\" на фільм \"{booking.Session.Film.Name}\" успішно видалено!";
                await _bookingService.SendBookingNotificationAsync(booking, "Deleted");
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.ViewerId == id);
        }
    }
}