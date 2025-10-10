using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarData()
        {
            var data = await _context.VueCalendarVrmPcs.ToListAsync();
            return Ok(data);
        }

        [HttpGet("test")]
       
        public IActionResult Test()
        {
            return Ok(new { message = "API OK" });
        }

        [HttpGet("headers")]
        public IActionResult GetHeaders()
        {
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var remoteUser = Request.Headers["X-Remote-User"].FirstOrDefault();

            return Ok(new
            {
                RemoteUser = remoteUser,
                AllHeaders = headers,
                Message = "Test réussi depuis Apache vers IIS"
            });
        }
    }
}
