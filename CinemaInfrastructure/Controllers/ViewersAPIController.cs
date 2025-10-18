using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class ViewersAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public ViewersAPIController(CinemaContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<bool> ViewerNameExistsAsync(string name, int? idToExclude = null)
        {
            var query = _context.Viewers.Where(v => v.Name == name);

            if (idToExclude.HasValue)
            {
                query = query.Where(v => v.Id != idToExclude.Value);
            }
            return await query.AnyAsync();
        }

        private bool CheckAge(DateOnly viewerAge, int minAge)
        {
            DateOnly result = DateOnly.FromDateTime(DateTime.Today).AddYears(-minAge);
            return viewerAge < result;
        }

        // GET: api/ViewersAPI
        [HttpGet]
        [Authorize(Roles = "superadmin")]
        public async Task<ActionResult<IEnumerable<Viewer>>> GetViewers()
        {
            return await _context.Viewers.Include(v => v.User).ToListAsync();
        }

        // GET: api/ViewersAPI/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Viewer>> GetViewer(int id)
        {
            var viewer = await _context.Viewers.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id);

            if (viewer == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (viewer.UserId != currentUserId && !User.IsInRole("superadmin"))
            {
                return StatusCode(403, new { Error = "Недостатньо прав доступу для перегляду профілю." });
            }

            return viewer;
        }

        // PUT: api/ViewersAPI/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutViewer(int id, Viewer viewer)
        {
            // 1. Перевірка ID та моделі
            if (id != viewer.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingViewer = await _context.Viewers.FindAsync(id);
            if (existingViewer == null)
            {
                return NotFound(new { Error = "Глядача не знайдено." });
            }

            var currentUserId = GetCurrentUserId();
            if (existingViewer.UserId != currentUserId && !User.IsInRole("superadmin") && !User.IsInRole("admin"))
            {
                return StatusCode(403, new { Error = "Недостатньо прав доступу для редагування профілю." });
            }

            if (!CheckAge(viewer.DateOfBirth, 14))
            {
                ModelState.AddModelError("DateOfBirth", "Мінімальний вік користувача має бути 14 років!");
            }
            if (await ViewerNameExistsAsync(viewer.Name, id))
            {
                ModelState.AddModelError("Name", "Користувач з таким ім'ям вже існує!");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            existingViewer.Name = viewer.Name;
            existingViewer.DateOfBirth = viewer.DateOfBirth;

            _context.Entry(existingViewer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні профілю.", Details = ex.Message });
            }

            return Ok(new { Status = "Ok" });
        }
    }
}
