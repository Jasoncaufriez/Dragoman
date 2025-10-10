using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    [HttpGet("current")]
    public IActionResult GetCurrentUser()
    {
        var login = Request.Headers["X-Remote-User"].ToString();
        if (string.IsNullOrWhiteSpace(login)) login = "anonymous";
        return Ok(new { username = login });
    }

    private readonly ApplicationDbContext _context;
    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("addUser")]
    public async Task<IActionResult> AddUser()
    {
        // Récupérer le nom d'utilisateur Windows
        var username = Environment.UserName;

        // Générer un nouvel ID pour cet enregistrement
        var maxId =  0; // Utiliser null-coalescing pour gérer le cas où la table est vide
        var newId = maxId + 1; // Incremental ID
        // Créer une nouvelle instance de Test avec l'ID et le nom d'utilisateur
        var newUserEntry = new Test
        {
            Id = newId,              // Assigner manuellement un nouvel ID
            TestVarchar = username
        };

       
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Utilisateur {username} ajouté avec l'ID {newId}." });
    }

}
