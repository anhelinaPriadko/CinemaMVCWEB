using CinemaDomain.Model;
using CinemaInfrastructure;
using CinemaInfrastructure.Pagination;
using CinemaInfrastructure.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [HttpGet(Name = "GetViewers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<PagedResponse<ViewerReadDTO>>> GetViewers([FromQuery] PaginationParameters parameters)
        {
            IQueryable<Viewer> viewersQuery = _context.Viewers
                .Include(v => v.User)
                .AsQueryable();
            viewersQuery = viewersQuery.OrderBy(v => v.Id);
            var totalCount = await viewersQuery.CountAsync();

            var paginatedViewersQuery = viewersQuery
                .Skip(parameters.Skip)
                .Take(parameters.Limit);

            var viewersDto = await paginatedViewersQuery
                .Select(v => new ViewerReadDTO
                {
                    Id = v.Id,
                    Name = v.Name,
                    DateOfBirth = v.DateOfBirth,
                    UserId = v.UserId,
                    User = new UserReadDTO
                    {
                        Id = v.User.Id,
                        UserName = v.User.UserName,
                        Email = v.User.Email,
                    }
                })
                .ToListAsync();
            const string routeName = "GetViewers";

            var nextLink = PaginationLinkHelper.CreateNextLink(
                Url,
                routeName,
                parameters,
                totalCount);

            var prevLink = PaginationLinkHelper.CreatePreviousLink(
                Url,
                routeName,
                parameters);

            return Ok(new PagedResponse<ViewerReadDTO>(
                viewersDto,
                totalCount,
                nextLink,
                prevLink));
        }

        // GET: api/ViewersAPI/5
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<ViewerReadDTO>> GetViewer(int id)
        {
            var viewer = await _context.Viewers
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (viewer == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (viewer.UserId != currentUserId && !User.IsInRole("superadmin"))
            {
                return StatusCode(403, new { Error = "Недостатньо прав доступу для перегляду профілю." });
            }

            var viewerDto = new ViewerReadDTO
            {
                Id = viewer.Id,
                Name = viewer.Name,
                DateOfBirth = viewer.DateOfBirth,
                UserId = viewer.UserId,
                User = new UserReadDTO
                {
                    Id = viewer.User.Id,
                    UserName = viewer.User.UserName,
                    Email = viewer.User.Email
                }
            };

            return viewerDto;
        }

        // PUT: api/ViewersAPI/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutViewer(int id, Viewer viewer)
        {
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
