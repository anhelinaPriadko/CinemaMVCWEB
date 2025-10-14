using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private record CountByFilmCategory(string cinemaType, int Count);
        private record CountByHalls(string cinemaType, int Count);

        private readonly CinemaContext cinemaContext;


        public ValuesController(CinemaContext cinemaContext)
        {
            this.cinemaContext = cinemaContext;
        }

        [HttpGet("countByType")]
        public async Task<JsonResult> GetCountByFilmCategoryAsync(CancellationToken cancellationToken)
        {
            var responseItems = await cinemaContext
                .Films
                .GroupBy(f => f.FilmCategory.Name)
                .Select(t => new { CinemaType = t.Key, Count = t.Count() })
                .ToListAsync(cancellationToken);

            var result = responseItems.Select(t => new CountByFilmCategory(t.CinemaType, t.Count)).ToList();

            return new JsonResult(result);
        }

        [HttpGet("countByHalls")]
        public async Task<JsonResult> GetCountByHallsAsync(CancellationToken cancellationToken)
        {
            var responseItems = await cinemaContext
                .Sessions
                .GroupBy(s => s.Hall.Name)
                .Select(t => new { Hall = t.Key, Count = t.Count() })
                .ToListAsync(cancellationToken);

            var result = responseItems.Select(t => new CountByHalls(t.Hall, t.Count)).ToList();

            return new JsonResult(result);
        }

    }
}
