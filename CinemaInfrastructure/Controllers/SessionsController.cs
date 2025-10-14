using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;

namespace CinemaInfrastructure.Controllers
{
    public enum SessionFilter
    {
        All,
        Past,
        Ongoing,
        Upcoming
    }

    public class SessionsController : Controller
    {
        private readonly CinemaContext _context;

        public SessionsController(CinemaContext context)
        {
            _context = context;
        }

        // GET: Sessions
        public async Task<IActionResult> Index(SessionFilter filter = SessionFilter.Upcoming)
        {
            IQueryable<Session> cinemaContext = _context.Sessions
                    .Include(s => s.Film)
                        .ThenInclude(f => f.Company)
                    .Include(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                    .Include(s => s.Hall)
                        .ThenInclude(h => h.HallType);

            cinemaContext = ApplySessionFilter(cinemaContext, filter)
                .OrderByDescending(s => s.SessionTime);

            ViewBag.SelectedFilter = filter;
            return View(await cinemaContext.ToListAsync());
        }

        public async Task<IActionResult> IndexByHall(int hallId, SessionFilter filter = SessionFilter.Upcoming)
        {
            var hallExists = await _context.Halls.AnyAsync(h => h.Id == hallId);
            if (!hallExists)
                return NotFound();

            IQueryable<Session> sessions = _context.Sessions
                    .Include(s => s.Film)
                        .ThenInclude(f => f.Company)
                    .Include(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                    .Include(s => s.Hall)
                        .ThenInclude(h => h.HallType);

            sessions = ApplySessionFilter(sessions, filter)
                .OrderByDescending(s => s.SessionTime);

            if (sessions.Count() == 0)
                TempData["Message"] = "На жаль, в цьому залі ще немає сеансів(";

            ViewBag.SelectedFilter = filter;
            return View("Index", sessions);
        }

        public async Task<IActionResult> IndexByFilm(int filmId, SessionFilter filter = SessionFilter.Upcoming)
        {
            var filmExists = await _context.Films.AnyAsync(f => f.Id == filmId);
            if (!filmExists)
                return NotFound();

            IQueryable<Session> sessions = _context.Sessions
                    .Include(s => s.Film)
                        .ThenInclude(f => f.Company)
                    .Include(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                    .Include(s => s.Hall)
                        .ThenInclude(h => h.HallType);

            sessions = ApplySessionFilter(sessions, filter)
                .OrderByDescending(s => s.SessionTime);

            if (sessions.Count() == 0)
                TempData["Message"] = "На жаль, для цього фільму ще немає сеансів(";

            ViewBag.SelectedFilter = filter;
            return View("Index", sessions);
        }

        private IQueryable<Session> ApplySessionFilter(IQueryable<Session> query, SessionFilter filter)
        {
            var now = DateTime.Now;

            return filter switch
            {
                SessionFilter.Past => query.Where(s => s.SessionTime.AddMinutes(s.Duration) < now),
                SessionFilter.Ongoing => query.Where(s => s.SessionTime <= now &&
                                                           s.SessionTime.AddMinutes(s.Duration) >= now),
                SessionFilter.Upcoming => query.Where(s => s.SessionTime > now),
                _ => query
            };
        }


        // GET: Sessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Film)
                .ThenInclude(f => f.Company)
                .Include(s => s.Film)
                .ThenInclude(f => f.FilmCategory)
                .Include(s => s.Hall)
                .ThenInclude(h => h.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        public async Task<IActionResult> DetailsByBookings(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Film)
                .ThenInclude(f => f.Company)
                .Include(s => s.Film)
                .ThenInclude(f => f.FilmCategory)
                .Include(s => s.Hall)
                .ThenInclude(h => h.HallType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexBySessions", "Bookings", new { sessionId = session.Id});
        }

        private bool checkTimeDuration(int hallId, DateTime sessionTime, int duration)
        {
            // Відсікаємо час, щоб отримати "початок доби"
            DateTime day = sessionTime.Date;
            DateTime nextDay = day.AddDays(1);

            var existingSessions = _context.Sessions
                .Where(s => s.HallId == hallId
                    && s.SessionTime >= day       // початок дня
                    && s.SessionTime < nextDay)   // початок наступного дня
                .OrderBy(s => s.SessionTime)
                .ToList();

            DateTime newSessionEnd = sessionTime.AddMinutes(duration);
            var previousSession = existingSessions.LastOrDefault(s => s.SessionTime < sessionTime);

            var nextSession = existingSessions.FirstOrDefault(s => s.SessionTime > sessionTime);

            if (previousSession != null)
            {
                DateTime prevSessionEnd = previousSession.SessionTime.AddMinutes(previousSession.Duration);
                if (sessionTime < prevSessionEnd)
                {
                    return false;
                }
            }

            if (nextSession != null)
            {
                DateTime nextSessionStart = nextSession.SessionTime;
                if (newSessionEnd > nextSessionStart)
                {
                    return false;
                }
            }

            return true; 
        }

        private bool checkClosingHours(DateTime sessionTime, int duration, TimeOnly startTime, TimeOnly endTime)
        {
            if (sessionTime.TimeOfDay > endTime.ToTimeSpan() || sessionTime.TimeOfDay < startTime.ToTimeSpan())
                return false;

            if (sessionTime.AddMinutes(duration).TimeOfDay > endTime.ToTimeSpan())
                return false;

            return true;
        }

        // GET: Sessions/Create
        public IActionResult Create()
        {
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name");
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name");
            return View();
        }

        // POST: Sessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FilmId,HallId,SessionTime,Duration,Id")] Session session)
        {
            Hall hall  = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(h => h.Id == session.HallId);

            Film film = await _context.Films
                .Include(f => f.FilmCategory)
                .Include(f => f.Company)
                .FirstOrDefaultAsync(f => f.Id == session.FilmId);

            session.Hall = hall;
            session.Film = film;

            ModelState.Clear();
            TryValidateModel(session);

            if (!ModelState.IsValid)
            {
                ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", session.FilmId);
                ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", session.HallId);
                return View(session);
            }

            if(!checkTimeDuration(session.HallId, session.SessionTime, session.Duration))
                ModelState.AddModelError("", "Неможливо додати сеанс! Неправильно обрано час початку та тривалість!");

            if(!checkClosingHours(session.SessionTime, session.Duration, new TimeOnly(8, 0), new TimeOnly(22,0)))
                ModelState.AddModelError("", "Сеанс поза робочими годинами кінотеатру!");

            if (ModelState.IsValid)
            {
                _context.Add(session);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", session.FilmId);
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", session.HallId);
            return View(session);
        }

        // GET: Sessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound();
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", session.FilmId);
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", session.HallId);
            return View(session);
        }

        // POST: Sessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FilmId,HallId,SessionTime,Duration,Id")] Session session)
        {
            if (id != session.Id)
            {
                return NotFound();
            }

            Film film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(f => f.Id == session.FilmId);

            Hall hall = await _context.Halls
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(h => h.Id == session.HallId);

            session.Hall = hall;
            session.Film = film;

            ModelState.Clear();
            TryValidateModel(session);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(session);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SessionExists(session.Id))
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
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", "Name", session.FilmId);
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", session.HallId);
            return View(session);
        }

        // GET: Sessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Film)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        // POST: Sessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions
                                .Include(s => s.Film)
                                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                TempData["ErrorMessage"] = "Сеанс не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Bookings.AnyAsync(b => b.SessionId == id);
            if(isLinked)
            {
                TempData["ErrorMessage"] = "Цей сеанс не можна видалити, оскільки він має пов'язані дані!";
                return RedirectToAction("Index");
            }

            string filmName = session.Film.Name;
            string filmDateTime = session.SessionTime.ToString("dd.MM.yyyy HH.mm");
            _context.Remove(session);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Сеанс фільму \"{filmName}\" \"{filmDateTime}\" успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.Id == id);
        }
    }
}
