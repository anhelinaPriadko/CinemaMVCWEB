using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CinemaInfrastructure.Controllers
{
    public class StatisticsController: Controller
    {
        [HttpGet]
        [Authorize(Roles = "superadmin")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
