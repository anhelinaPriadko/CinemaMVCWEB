using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsAPIController : ControllerBase
    {
        private readonly CinemaContext _context;
        private static readonly TimeOnly StartTime = new TimeOnly(8, 0);
        private static readonly TimeOnly EndTime = new TimeOnly(22, 0);

        public SessionsAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> SessionExistsAsync(int id)
        {
            return await _context.Sessions.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> FilmExistsAsync(int id)
        {
            return await _context.Films.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> HallExistsAsync(int id)
        {
            return await _context.Halls.AnyAsync(e => e.Id == id);
        }

        private bool CheckClosingHours(DateTime sessionTime, int duration)
        {
            TimeSpan sessionStart = sessionTime.TimeOfDay;
            TimeSpan sessionEnd = sessionTime.AddMinutes(duration).TimeOfDay;

            if (sessionStart >= EndTime.ToTimeSpan() || sessionStart < StartTime.ToTimeSpan())
                return false;
            if (sessionEnd > EndTime.ToTimeSpan())
                return false;

            return true;
        }

        private async Task<bool> CheckTimeDurationAsync(int hallId, DateTime sessionTime, int duration, int? sessionIdToExclude = null)
        {
            DateTime newSessionEnd = sessionTime.AddMinutes(duration);
            DateTime day = sessionTime.Date;
            DateTime nextDay = day.AddDays(1);

            var existingSessionsQuery = _context.Sessions
                .Where(s => s.HallId == hallId
                         && s.SessionTime >= day
                         && s.SessionTime < nextDay);

            if (sessionIdToExclude.HasValue)
            {
                existingSessionsQuery = existingSessionsQuery.Where(s => s.Id != sessionIdToExclude.Value);
            }

            var existingSessions = await existingSessionsQuery.ToListAsync();

            foreach (var existing in existingSessions)
            {
                DateTime existingStart = existing.SessionTime;
                DateTime existingEnd = existing.SessionTime.AddMinutes(existing.Duration);

                if (sessionTime < existingEnd && newSessionEnd > existingStart)
                {
                    return false;
                }
            }

            return true;
        }

        private IQueryable<Session> GetSessionsQuery()
        {
            return _context.Sessions
                .Include(s => s.Film)
                    .ThenInclude(f => f.Company)
                .Include(s => s.Film)
                    .ThenInclude(f => f.FilmCategory)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.HallType);
        }

        // GET: api/Sessions1
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            return await GetSessionsQuery()
                .Where(s => s.SessionTime > DateTime.Now)
                .OrderBy(s => s.SessionTime)
                .ToListAsync();
        }

        //повернення майбутніх сеансів для певного залу
        [HttpGet("UpcomingByHall/{hallId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Session>>> GetUpcomingSessionsByHall(int hallId)
        {
            if (!await HallExistsAsync(hallId))
            {
                return NotFound(new { Error = "Зал не знайдено." });
            }

            return await GetSessionsQuery()
                .Where(s => s.HallId == hallId && s.SessionTime > DateTime.Now)
                .OrderBy(s => s.SessionTime)
                .ToListAsync();
        }

        //повернення майбутніх сеансів за певним фільмом
        [HttpGet("UpcomingByFilm/{filmId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Session>>> GetUpcomingSessionsByFilm(int filmId)
        {
            if (!await FilmExistsAsync(filmId))
            {
                return NotFound(new { Error = "Фільм не знайдено." });
            }

            return await GetSessionsQuery()
                .Where(s => s.FilmId == filmId && s.SessionTime > DateTime.Now)
                .OrderBy(s => s.SessionTime)
                .ToListAsync();
        }

        // GET: api/Sessions1/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            var session = await GetSessionsQuery().FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            return session;
        }

        // PUT: api/Sessions1/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutSession(int id, Session session)
        {
            if (id != session.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await FilmExistsAsync(session.FilmId) || !await HallExistsAsync(session.HallId))
            {
                return BadRequest(new { Error = "Вказаний фільм або зал не знайдено." });
            }

            var existingSession = await _context.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (existingSession == null)
            {
                return NotFound(new { Error = "Сеанс не знайдено." });
            }

            var hasBookings = await _context.Bookings.AnyAsync(b => b.SessionId == id);
            if (hasBookings)
            {
                return BadRequest(new { Error = "Неможливо редагувати сеанс, оскільки на нього вже є бронювання." });
            }

            if (!CheckClosingHours(session.SessionTime, session.Duration))
            {
                return BadRequest(new { Error = "Сеанс поза робочими годинами кінотеатру (8:00 - 22:00)." });
            }

            if (!await CheckTimeDurationAsync(session.HallId, session.SessionTime, session.Duration, id))
            {
                return BadRequest(new { Error = "Неможливо оновити сеанс! Він накладається на інший сеанс у цьому залі." });
            }

            _context.Entry(session).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SessionExistsAsync(id))
                {
                    return NotFound(new { Error = "Сеанс не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні сеансу.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }

        // POST: api/Sessions1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<Session>> PostSession(Session session)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await FilmExistsAsync(session.FilmId) || !await HallExistsAsync(session.HallId))
            {
                return BadRequest(new { Error = "Вказаний фільм або зал не знайдено." });
            }

            if (!CheckClosingHours(session.SessionTime, session.Duration))
            {
                return BadRequest(new { Error = "Сеанс поза робочими годинами кінотеатру (8:00 - 22:00)." });
            }

            if (!await CheckTimeDurationAsync(session.HallId, session.SessionTime, session.Duration))
            {
                return BadRequest(new { Error = "Неможливо додати сеанс! Він накладається на інший сеанс у цьому залі." });
            }

            _context.Sessions.Add(session);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні сеансу.", Details = ex.Message });
            }

            return CreatedAtAction("GetSession", new { id = session.Id }, new { Status = "Ok" });
        }

        // DELETE: api/Sessions1/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var isLinked = await _context.Bookings.AnyAsync(b => b.SessionId == id);
            if (isLinked)
            {
                return BadRequest(new { Error = "Цей сеанс не можна видалити, оскільки на нього є бронювання." });
            }

            _context.Sessions.Remove(session);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при видаленні сеансу.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }
    }
}
