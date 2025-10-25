using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CinemaInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesAPIController : ControllerBase
    {
        private readonly CinemaContext _context;

        public CompaniesAPIController(CinemaContext context)
        {
            _context = context;
        }

        private async Task<bool> CompanyExistsAsync(int id)
        {
            return await _context.Companies.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> CompanyNameExistsAsync(string name)
        {
            return await _context.Companies.AnyAsync(e => e.Name.ToLower() == name.ToLower());
        }

        // GET: api/CompaniesAPI
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies.ToListAsync();
        }

        // GET: api/CompaniesAPI/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Company>> GetCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);

            if (company == null)
            {
                return NotFound();
            }

            return company;
        }

        // PUT: api/CompaniesAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> PutCompany(int id, Company company)
        {
            if (id != company.Id)
            {
                return BadRequest(new { Error = "ID у маршруті не відповідає ID у тілі запиту." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingCompany == null)
            {
                return NotFound(new { Error = "Виробника не знайдено." });
            }

            if (existingCompany.Name.ToLower() != company.Name.ToLower() && await CompanyNameExistsAsync(company.Name))
            {
                return BadRequest(new { Error = $"Виробник з назвою '{company.Name}' вже існує." });
            }

            _context.Entry(company).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CompanyExistsAsync(id))
                {
                    return NotFound(new { Error = "Виробника не знайдено (можливо, щойно видалено)." });
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при оновленні виробника.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }

        // POST: api/CompaniesAPI
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<ActionResult<Company>> PostCompany(Company company)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await CompanyNameExistsAsync(company.Name))
            {
                return BadRequest(new { Error = $"Виробник з назвою '{company.Name}' вже існує." });
            }

            _context.Companies.Add(company);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Error = "Помилка при збереженні виробника.", Details = ex.Message });
            }
            return CreatedAtAction("GetCompany", new { id = company.Id }, new { Status = "Ok", id = company.Id });
        }

        // DELETE: api/CompaniesAPI/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "superadmin")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            _context.Companies.Remove(company);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Error = "Неможливо видалити виробника, оскільки він має пов'язані фільми.", Details = ex.Message });
            }
            return Ok(new { Status = "Ok" });
        }
    }
}
