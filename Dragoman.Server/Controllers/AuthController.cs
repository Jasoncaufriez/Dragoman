using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;



[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI()
    {
        var name = User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
            return Content(name);

        // fallback optionnel
        var header = Request.Headers["X-Remote-User"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
            return Content(header);

        // sinon, force le handshake
        return Challenge(IISDefaults.AuthenticationScheme);
    }
}