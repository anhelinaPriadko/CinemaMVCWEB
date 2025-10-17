using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;

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

        // GET: api/HallsAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hall>>> GetHalls()
        {
            return await _context.Halls.Include(h => h.HallType).ToListAsync();
        }

        // GET: api/HallsAPI/5
        [HttpGet("{id}")]
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

            if (!await HallTypeExistsAsync(hall.HallTypeId))
            {
                return BadRequest(new { Error = "Вказаний тип залу не знайдено." });
            }

            var existingHall = await _context.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);

            if (existingHall == null)
            {
                return NotFound(new { Error = "Зал не знайдено." });
            }

            if (existingHall.Name.ToLower() != hall.Name.ToLower() && await HallNameExistsAsync(hall.Name))
            {
                return BadRequest(new { Error = $"Зал з назвою '{hall.Name}' вже існує." });
            }

            _context.Entry(hall).State = EntityState.Modified;

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

            _context.Halls.Add(hall);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні залу.", Details = ex.Message });
            }

            return CreatedAtAction("GetHall", new { id = hall.Id }, new { Status = "Ok" });
        }

        // DELETE: api/HallsAPI/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHall(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                return NotFound();
            }

            _context.Halls.Remove(hall);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Error = "Неможливо видалити зал, оскільки він має пов'язані місця чи сеанси.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }
    }
}
