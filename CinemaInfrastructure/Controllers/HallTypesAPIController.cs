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
    public class HallTypesAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public HallTypesAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> HallTypeExistsAsync(int id)
        {
            return await _context.HallTypes.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> HallTypeNameExistsAsync(string name)
        {
            return await _context.HallTypes.AnyAsync(e => e.Name.ToLower() == name.ToLower());
        }

        // GET: api/HallTypesAPI
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<HallType>>> GetHallTypes()
        {
            return await _context.HallTypes.ToListAsync();
        }

        // GET: api/HallTypesAPI/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<HallType>> GetHallType(int id)
        {
            var hallType = await _context.HallTypes.FindAsync(id);

            if (hallType == null)
            {
                return NotFound();
            }

            return hallType;
        }

        // PUT: api/HallTypesAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutHallType(int id, HallType hallType)
        {
            if (id != hallType.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingHallType = await _context.HallTypes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (existingHallType == null)
            {
                return NotFound(new { Error = "Тип залу не знайдено." });
            }

            if (existingHallType.Name.ToLower() != hallType.Name.ToLower() && await HallTypeNameExistsAsync(hallType.Name))
            {
                return BadRequest(new { Error = $"Тип залу з назвою '{hallType.Name}' вже існує." });
            }

            _context.Entry(hallType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HallTypeExistsAsync(id))
                {
                    return NotFound(new { Error = "Тип залу не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні типу залу.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }

        // POST: api/HallTypesAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<HallType>> PostHallType(HallType hallType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await HallTypeNameExistsAsync(hallType.Name))
            {
                return BadRequest(new { Error = $"Тип залу з назвою '{hallType.Name}' вже існує." });
            }

            _context.HallTypes.Add(hallType);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні типу залу.", Details = ex.Message });
            }

            return CreatedAtAction("GetHallType", new { id = hallType.Id }, new { Status = "Ok", id = hallType.Id });
        }

        // DELETE: api/HallTypesAPI/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteHallType(int id)
        {
            var hallType = await _context.HallTypes.FindAsync(id);
            if (hallType == null)
            {
                return NotFound();
            }

            _context.HallTypes.Remove(hallType);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Error = "Неможливо видалити тип залу, оскільки на нього посилаються існуючі зали.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }
    }
}
