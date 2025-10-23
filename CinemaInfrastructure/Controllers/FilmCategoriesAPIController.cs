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
    public class FilmCategoriesAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public FilmCategoriesAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> FilmCategoryExistsAsync(int id)
        {
            return await _context.FilmCategories.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> FilmCategoryNameExistsAsync(string name)
        {
            return await _context.FilmCategories.AnyAsync(e => e.Name.ToLower() == name.ToLower());
        }

        // GET: api/FilmCategoriesAPI
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<FilmCategory>>> GetFilmCategories()
        {
            return await _context.FilmCategories.ToListAsync();
        }

        // GET: api/FilmCategoriesAPI/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<FilmCategory>> GetFilmCategory(int id)
        {
            var filmCategory = await _context.FilmCategories.FindAsync(id);

            if (filmCategory == null)
            {
                return NotFound();
            }

            return filmCategory;
        }

        // PUT: api/FilmCategoriesAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutFilmCategory(int id, FilmCategory filmCategory)
        {
            if (id != filmCategory.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCategory = await _context.FilmCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (existingCategory == null)
            {
                return NotFound(new { Error = "Категорію не знайдено." });
            }

            if (existingCategory.Name.ToLower() != filmCategory.Name.ToLower() && await FilmCategoryNameExistsAsync(filmCategory.Name))
            {
                return BadRequest(new { Error = $"Категорія з назвою '{filmCategory.Name}' вже існує." });
            }

            _context.Entry(filmCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await FilmCategoryExistsAsync(id))
                {
                    return NotFound(new { Error = "Категорію не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні категорії.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }

        // POST: api/FilmCategoriesAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<FilmCategory>> PostFilmCategory(FilmCategory filmCategory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await FilmCategoryNameExistsAsync(filmCategory.Name))
            {
                return BadRequest(new { Error = $"Категорія з назвою '{filmCategory.Name}' вже існує." });
            }

            _context.FilmCategories.Add(filmCategory);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні категорії.", Details = ex.Message });
            }
            return CreatedAtAction("GetFilmCategory", new { id = filmCategory.Id }, new { Status = "Ok" });
        }

        // DELETE: api/FilmCategoriesAPI/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteFilmCategory(int id)
        {
            var filmCategory = await _context.FilmCategories.FindAsync(id);
            if (filmCategory == null)
            {
                return NotFound();
            }

            _context.FilmCategories.Remove(filmCategory);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Error = "Неможливо видалити категорію, оскільки на неї посилаються фільми.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }
    }
}
