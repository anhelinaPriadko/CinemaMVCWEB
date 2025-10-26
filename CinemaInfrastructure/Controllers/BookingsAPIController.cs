using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BookingsAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<int?> GetViewerIdByUserIdAsync(string userId)
        {
            var viewer = await _context.Viewers.FirstOrDefaultAsync(v => v.UserId == userId);
            return viewer?.Id;
        }

        private async Task<bool> BookingExistsAsync(int sessionId, int seatId)
        {
            return await _context.Bookings.AnyAsync(b =>
                        b.SessionId == sessionId
                        && b.SeatId == seatId);
        }

        public BookingsAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<ActionResult<Booking>?> ValidateBookingCreationAsync(int sessionId, int seatId)
        {
            var session = await _context.Sessions
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return BadRequest(new { Error = "Сеанс з таким ID не знайдено." });
            }

            var currentTime = DateTime.Now;
            var sessionStartTime = session.SessionTime;

            TimeSpan maxDelay = TimeSpan.FromMinutes(20);
            DateTime allowedEndTime = sessionStartTime.Add(maxDelay);
            if (currentTime > allowedEndTime)
            {
                DateTime sessionFinishTime = sessionStartTime.AddMinutes(session.Duration);
                if (currentTime > sessionFinishTime)
                {
                    return BadRequest(new { Error = "Сеанс закінчився." });
                }
                return BadRequest(new { Error = "Час для бронювання вичерпано (дозволено лише перші 20 хвилин сеансу)." });
            }

            var seatExists = await _context.Seats.AnyAsync(s =>
                s.Id == seatId && s.HallId == session.HallId);

            if (!seatExists)
            {
                return BadRequest(new { Error = "Місце з таким ID не знайдено в залі для цього сеансу." });
            }

            return null;
        }


        // GET: api/BookingsAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            IQueryable<Booking> bookingsQuery = _context.Bookings
                .Include(b => b.Seat)
                    .ThenInclude(s => s.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .Include(b => b.Viewer);

            if (User.IsInRole("user"))
            {
                var currentUserId = GetCurrentUserId();
                bookingsQuery = bookingsQuery.Where(b => b.Viewer.UserId == currentUserId);
            }
            var bookings = await bookingsQuery.ToListAsync();
            return bookings;
        }

        // GET: api/BookingsAPI/1/10/2 (ViewerId, SessionId, SeatId)
        [HttpGet("{viewerId}/{sessionId}/{seatId}")]
        public async Task<ActionResult<Booking>> GetBooking(int viewerId, int sessionId, int seatId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Viewer)
                .Include(b => b.Seat)
                    .ThenInclude(b => b.Hall)
                        .ThenInclude(h => h.HallType)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.FilmCategory)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Film)
                        .ThenInclude(f => f.Company)
                .FirstOrDefaultAsync(
                    b =>
                        b.ViewerId == viewerId
                        && b.SessionId == sessionId
                        && b.SeatId == seatId
                );

            if (booking == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("superadmin") && !User.IsInRole("admin"))
            {
                var currentViewerId = await GetViewerIdByUserIdAsync(GetCurrentUserId());

                if (viewerId != currentViewerId)
                {
                    return StatusCode(403, new { Error = "Недостатньо прав для перегляду цього бронювання." });
                }
            }

            return booking;
        }

        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // PUT: api/BookingsApi/1/10/2 (ViewerId, SessionId, SeatId)
        [HttpPut("{viewerId}/{sessionId}/{seatId}")]
        public async Task<IActionResult> PutBooking(int viewerId, int sessionId, int seatId, [FromBody] Booking booking)
        {
            if (viewerId != booking.ViewerId || sessionId != booking.SessionId)
            {
                return BadRequest(new { Error = "В бронюванні можливо змінити лише місце на обраний сеанс" });
            }

            var oldBooking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ViewerId == viewerId && b.SessionId == sessionId && b.SeatId == seatId);

            if (oldBooking == null)
            {
                return NotFound(new { Error = "Бронювання не знайдено." });
            }

            if (seatId == booking.SeatId)
            {
                return Ok(new { Status = "Ok", Message = "Бронювання місця не вимагає оновлення." });
            }

            var validationResult = await ValidateBookingCreationAsync(sessionId, booking.SeatId);
            if (validationResult != null)
            {
                if (validationResult.Result != null)
                    return validationResult.Result;

                return BadRequest(new { Error = "Введено не валідні дані." });
            }

            if (await BookingExistsAsync(sessionId, booking.SeatId))
            {
                return BadRequest(new { Error = "Обране місце вже заброньовано!" });
            }

            var currentViewerId = await GetViewerIdByUserIdAsync(GetCurrentUserId());

            if (viewerId != currentViewerId && !User.IsInRole("superadmin") && !User.IsInRole("admin"))
            {
                return StatusCode(403, new { Error = "Неможливо редагувати бронювання іншого користувача." });
            }

            _context.Bookings.Remove(oldBooking);
            var newBooking = new Booking
            {
                ViewerId = viewerId,
                SessionId = sessionId,
                SeatId = booking.SeatId
            };
            _context.Bookings.Add(newBooking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні нового бронювання.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }

        // POST: api/BookingsAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Booking>> PostBooking([FromBody] Booking booking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var validationResult = await ValidateBookingCreationAsync(booking.SessionId, booking.SeatId);
            if (validationResult != null)
            {
                return validationResult;
            }
            if (await BookingExistsAsync(booking.SessionId, booking.SeatId))
            {
                return BadRequest(new { Error = "Обране місце вже заброньовано!" });
            }

            int finalViewerId;

            if (User.IsInRole("superadmin"))
            {
                if (booking.ViewerId <= 0 || !await _context.Viewers.AnyAsync(v => v.Id == booking.ViewerId))
                {
                    return BadRequest(new { Error = "Некоректно вказано глядача для створення бронювання." });
                }
                finalViewerId = booking.ViewerId;
            }
            else
            {
                
                var currentViewerId = await GetViewerIdByUserIdAsync(GetCurrentUserId()!);

                if (currentViewerId == null)
                {
                    return Unauthorized(new { Error = "Профіль глядача не знайдено." });
                }
                finalViewerId = currentViewerId.Value;

                if (booking.ViewerId != 0 && booking.ViewerId != finalViewerId)
                {
                    return StatusCode(403, new { Error = "Не дозволено створювати бронювання від імені іншого глядача." });
                }
            }

            var newBooking = new Booking
            {
                ViewerId = finalViewerId,
                SessionId = booking.SessionId,
                SeatId = booking.SeatId
            };

            _context.Bookings.Add(newBooking);
            try
            {
                await _context.SaveChangesAsync();
            } 
            catch(DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні нового бронювання.", Details = ex.Message });
            }
            return CreatedAtAction("GetBooking",
                new { viewerId = newBooking.ViewerId, sessionId = newBooking.SessionId, seatId = newBooking.SeatId },
                new { Status = "Ok", viewerId = newBooking.ViewerId, sessionId = newBooking.SessionId, seatId = newBooking.SeatId });
        }

        // DELETE: api/BookingsAPI/1/10/2 (ViewerId, SessionId, SeatId)
        [HttpDelete("{viewerId}/{sessionId}/{seatId}")]
        public async Task<IActionResult> DeleteBooking(int viewerId, int sessionId, int seatId)
        {
            var booking = await _context.Bookings.FindAsync(viewerId, sessionId, seatId);
            if (booking == null)
            {
                return NotFound();
            }

            var currentViewerId = await GetViewerIdByUserIdAsync(GetCurrentUserId());

            if (viewerId != currentViewerId && !User.IsInRole("superadmin"))
            {
                return StatusCode(403, new { Error = "Неможливо видалити бронювання іншого користувача." });
            }

            _context.Bookings.Remove(booking);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні нового бронювання.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }
    }
}
