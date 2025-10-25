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
    public class HallsAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public HallsAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> HallExistsAsync(int id)
        {
            return await _context.Halls.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> HallNameExistsAsync(string name)
        {
            return await _context.Halls.AnyAsync(e => e.Name.ToLower() == name.ToLower());
        }

        private async Task<bool> HallTypeExistsAsync(int hallTypeId)
        {
            return await _context.HallTypes.AnyAsync(e => e.Id == hallTypeId);
        }

        private List<Seat> GenerateSeats(int numberOfRows, int seatsInRow)
        {
            var seats = new List<Seat>();
            for (int row = 1; row <= numberOfRows; row++)
            {
                for (int seatNum = 1; seatNum <= seatsInRow; seatNum++)
                {
                    seats.Add(new Seat
                    {
                        Row = row,
                        NumberInRow = seatNum
                    });
                }
            }
            return seats;
        }

        private async Task<string?> AddOrUpdateSeatsAsync(Hall hall, int newNumberOfRows, int newSeatsInRow)
        {
            await _context.Entry(hall).Collection(h => h.Seats).LoadAsync();
            var oldSeats = hall.Seats.ToList();

            var bookedSeats = await _context.Bookings
                .Where(b => b.Seat.HallId == hall.Id)
                .Select(b => b.SeatId)
                .Distinct()
                .ToListAsync();

            if (newNumberOfRows < hall.NumberOfRows || newSeatsInRow < hall.SeatsInRow)
            {
                var seatsToBeRemoved = oldSeats
                    .Where(s => s.Row > newNumberOfRows || s.NumberInRow > newSeatsInRow)
                    .ToList();

                if (seatsToBeRemoved.Any(s => bookedSeats.Contains(s.Id)))
                {
                    return "Неможливо зменшити розмір залу, оскільки деякі місця вже заброньовані!";
                }
                _context.Seats.RemoveRange(seatsToBeRemoved);
            }

            for (int row = 1; row <= newNumberOfRows; row++)
            {
                for (int seatNum = 1; seatNum <= newSeatsInRow; seatNum++)
                {
                    if (!oldSeats.Any(s => s.Row == row && s.NumberInRow == seatNum))
                    {
                        _context.Seats.Add(new Seat { HallId = hall.Id, Row = row, NumberInRow = seatNum });
                    }
                }
            }

            hall.NumberOfRows = newNumberOfRows;
            hall.SeatsInRow = newSeatsInRow;

            return null;
        }

        // GET: api/HallsAPI
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Hall>>> GetHalls()
        {
            return await _context.Halls.Include(h => h.HallType).ToListAsync();
        }

        // GET: api/HallsAPI/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Hall>> GetHall(int id)
        {
            var hall = await _context.Halls.Include(h => h.HallType).FirstOrDefaultAsync(h => h.Id == id);

            if (hall == null)
            {
                return NotFound();
            }

            return hall;
        }

        // PUT: api/HallsAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutHall(int id, Hall hall)
        {
            if (id != hall.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingHall = await _context.Halls
                .Include(h => h.Seats)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (existingHall == null)
            {
                return NotFound(new { Error = "Зал не знайдено." });
            }

            if (existingHall.Name.ToLower() != hall.Name.ToLower() && await HallNameExistsAsync(hall.Name))
            {
                return BadRequest(new { Error = $"Зал з назвою '{hall.Name}' вже існує." });
            }

            if (!await HallTypeExistsAsync(hall.HallTypeId))
            {
                return BadRequest(new { Error = "Вказаний тип залу не знайдено." });
            }

            var hallToUpdate = await _context.Halls.Include(h => h.Seats).FirstAsync(h => h.Id == id);

            var error = await AddOrUpdateSeatsAsync(hallToUpdate, hall.NumberOfRows, hall.SeatsInRow);
            if (error != null)
            {
                return BadRequest(new { Error = error });
            }

            hallToUpdate.Name = hall.Name;
            hallToUpdate.HallTypeId = hall.HallTypeId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HallExistsAsync(id))
                {
                    return NotFound(new { Error = "Зал не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні залу.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }

        // POST: api/HallsAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<Hall>> PostHall(Hall hall)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await HallNameExistsAsync(hall.Name))
            {
                return BadRequest(new { Error = $"Зал з назвою '{hall.Name}' вже існує." });
            }

            if (!await HallTypeExistsAsync(hall.HallTypeId))
            {
                return BadRequest(new { Error = "Вказаний тип залу не знайдено." });
            }

            hall.Seats = GenerateSeats(hall.NumberOfRows, hall.SeatsInRow);
            _context.Halls.Add(hall);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні залу.", Details = ex.Message });
            }

            return CreatedAtAction("GetHall", new { id = hall.Id }, new { Status = "Ok", id = hall.Id });
        }

        // DELETE: api/HallsAPI/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteHall(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                return NotFound();
            }

            var futureSessionsExist = await _context.Sessions
                .AnyAsync(s => s.HallId == id && s.SessionTime > DateTime.Now);

            var hasBookings = await _context.Bookings.AnyAsync(b => b.Seat.HallId == id);

            if (futureSessionsExist || hasBookings)
            {
                return BadRequest(new { Error = "Цей зал не можна видалити, оскільки він має пов'язані сеанси або бронювання." });
            }

            var seatsToRemove = await _context.Seats.Where(s => s.HallId == id).ToListAsync();
            _context.Seats.RemoveRange(seatsToRemove);

            _context.Halls.Remove(hall);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Неможливо видалити зал через непередбачені пов'язані дані.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }
    }
}
