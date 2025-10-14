using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles= "superadmin")]
    public class CompaniesController : Controller
    {
        private readonly CinemaContext _context;

        public CompaniesController(CinemaContext context)
        {
            _context = context;
        }

        // GET: Companies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Companies.ToListAsync());
        }

        // GET: Companies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexByCompany", "Films", new { companyId = company.Id });
        }

        public bool CheckNameDublication(string Name)
        {
            return _context.Companies.Any(m => m.Name == Name);
        }

        // GET: Companies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] Company company)
        {
            if (CheckNameDublication(company.Name))
            {
                ModelState.AddModelError("Name", "Компанія з такою назвою вже існує!");
            }

            if (!ModelState.IsValid) 
            {
                return View(company);
            }

            _context.Add(company);
            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Index));
        }
        // GET: Companies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: Companies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            var existingCompany = await _context.Companies.FindAsync(id);
            if (existingCompany == null)
                return NotFound();

            if (existingCompany.Name != company.Name && CheckNameDublication(company.Name))
            {
                ModelState.AddModelError("Name", "Виробник з такою назвою вже існує!");
            }

            if (!ModelState.IsValid)
            {
                return View(company);
            }

            try
            {
                existingCompany.Name = company.Name;
                _context.Update(existingCompany);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(company.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Companies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                TempData["ErrorMessage"] = "Компанію не знайдено!";
                return RedirectToAction("Index");
            }

            var isLinked = await _context.Films.AnyAsync(f => f.CompanyId == id);

            if(isLinked)
            {
                TempData["ErrorMessage"] = "Цю компанію не можна видалити, оскільки вона має пов'язані дані!";
                return RedirectToAction("Index");
            }

            _context.Remove(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Компанію \"{company.Name}\" успішно видалено!";
            return RedirectToAction("Index");
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
