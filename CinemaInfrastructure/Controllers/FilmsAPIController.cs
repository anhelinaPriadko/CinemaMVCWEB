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
    public class FilmsAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public FilmsAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> FilmExistsAsync(int id)
        {
            return await _context.Films.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> FilmNameExistsAsync(string name)
        {
            return await _context.Films.AnyAsync(e => e.Name.ToLower() == name.ToLower());
        }

        private async Task<bool> CompanyExistsAsync(int id)
        {
            return await _context.Companies.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> FilmCategoryExistsAsync(int id)
        {
            return await _context.FilmCategories.AnyAsync(e => e.Id == id);
        }

        // GET: api/FilmsAPI
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Film>>> GetFilms()
        {
            return await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .ToListAsync();
        }

        // GET: api/FilmsAPI/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Film>> GetFilm(int id)
        {
            var film = await _context.Films
                .Include(f => f.Company)
                .Include(f => f.FilmCategory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film == null)
            {
                return NotFound();
            }

            return film;
        }

        // PUT: api/FilmsAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutFilm(int id, Film film)
        {
            if (id != film.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await CompanyExistsAsync(film.CompanyId))
            {
                return BadRequest(new { Error = "Вказаний виробник (CompanyId) не знайдено." });
            }
            if (!await FilmCategoryExistsAsync(film.FilmCategoryId))
            {
                return BadRequest(new { Error = "Вказана категорія фільму (FilmCategoryId) не знайдена." });
            }

            var existingFilm = await _context.Films.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

            if (existingFilm == null)
            {
                return NotFound(new { Error = "Фільм не знайдено." });
            }
            if (existingFilm.Name.ToLower() != film.Name.ToLower() && await FilmNameExistsAsync(film.Name))
            {
                return BadRequest(new { Error = $"Фільм з назвою '{film.Name}' вже існує." });
            }

            _context.Entry(film).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await FilmExistsAsync(id))
                {
                    return NotFound(new { Error = "Фільм не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні фільму.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }

        // POST: api/FilmsAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<Film>> PostFilm(Film film)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await FilmNameExistsAsync(film.Name))
            {
                return BadRequest(new { Error = $"Фільм з назвою '{film.Name}' вже існує." });
            }

            if (!await CompanyExistsAsync(film.CompanyId))
            {
                return BadRequest(new { Error = "Вказаний виробник не знайдено." });
            }
            if (!await FilmCategoryExistsAsync(film.FilmCategoryId))
            {
                return BadRequest(new { Error = "Вказана категорія фільму не знайдена." });
            }

            if (string.IsNullOrEmpty(film.PosterPath))
            {
                film.PosterPath = "/img/empty_film_image.png";
            }

            _context.Films.Add(film);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні фільму.", Details = ex.Message });
            }
            return CreatedAtAction("GetFilm", new { id = film.Id }, new { Status = "Ok", id = film.Id });
        }

        // DELETE: api/FilmsAPI/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteFilm(int id)
        {
            var film = await _context.Films.FindAsync(id);
            if (film == null)
            {
                return NotFound();
            }

            _context.Films.Remove(film);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Error = "Неможливо видалити фільм, оскільки на нього посилаються сеанси.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }
    }
}
