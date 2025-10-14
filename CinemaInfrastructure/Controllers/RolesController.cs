using CinemaDomain.Model;
using CinemaInfrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // Метод для відображення списку користувачів
        public IActionResult UserList() => View(_userManager.Users.ToList());

        // GET: Завантаження форми редагування ролі для конкретного користувача
        [HttpGet]
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Отримуємо поточну роль (якщо декілька, беремо першу)
                var roles = await _userManager.GetRolesAsync(user);
                string currentRole = roles.FirstOrDefault() ?? "user";

                var allRoles = _roleManager.Roles.ToList();
                ChangeRoleViewModel model = new ChangeRoleViewModel
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    SelectedRole = currentRole,
                    AllRoles = allRoles
                };
                return View(model);
            }
            return NotFound();
        }

        // POST: Обробка редагування ролі для користувача
        [HttpPost]
        public async Task<IActionResult> Edit(string userId, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, selectedRole);

                return RedirectToAction("UserList");
            }
            return NotFound();
        }
    }

}
