using System.Data;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class LanguesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LanguesController(ApplicationDbContext db) => _db = db;

    // ---------------- Référentiel langues ----------------
    // GET /api/langues[?destOnly=true]
    [HttpGet("langues")]
    public async Task<ActionResult<IEnumerable<LangueDto>>> GetAllLangues(
        [FromQuery] bool destOnly = false,
        CancellationToken ct = default)
    {
        var q = _db.Langues.AsNoTracking();

        // 👇 filtre demandé : seulement les langues dont IslangueDestination n’est PAS null
        if (destOnly)
            q = q.Where(l => l.IslangueDestination != null);

        var rows = await q
            .OrderBy(l => l.LibelleFr)
            .Select(l => new LangueDto
            {
                Idlangue = l.Idlangue,
                CodeIso = l.CodeIso,
                LibelleFr = l.LibelleFr,
                LibelleNl = l.LibelleNl
            })
            .ToListAsync(ct);

        return Ok(rows);
    }


    // ---------------- Langues SOURCE par interprète ----------------
    // GET /api/interpretes/{tolkcode}/langues/sources
    [HttpGet("interpretes/{tolkcode:int}/langues/sources")]
    public async Task<IActionResult> GetSources(int tolkcode, CancellationToken ct)
    {
        var q =
            from ls in _db.LangueSources.AsNoTracking()
            where ls.Tolkcode == tolkcode
            join lg in _db.Langues.AsNoTracking()
                on ls.NrLangue equals (int?)lg.Idlangue into gj
            from lg in gj.DefaultIfEmpty()
            orderby lg.LibelleFr, lg.LibelleNl
            select new LangueSourceDto
            {
                IdLangueSource = ls.IdLanguesource,
                Tolkcode = ls.Tolkcode,
                NrLangue = ls.NrLangue ?? 0,
                LibelleFr = lg.LibelleFr,
                LibelleNl = lg.LibelleNl
            };

        return Ok(await q.ToListAsync(ct));
    }

    // POST /api/interpretes/{tolkcode}/langues/source
    [HttpPost("interpretes/{tolkcode:int}/langues/source")]
    public async Task<IActionResult> AddSource(int tolkcode, [FromBody] AddLangueDto body, CancellationToken ct)
    {
        if (body is null || body.NrLangue is null || body.NrLangue <= 0)
            return BadRequest("NrLangue requis.");

        // FIX: Utiliser FirstOrDefaultAsync au lieu de AnyAsync pour éviter les problèmes Oracle
        var tolkExists = await _db.Tolkidentities.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tolkcode == tolkcode, ct);
        if (tolkExists == null) return NotFound($"Interprète {tolkcode} introuvable.");

        var langueId = (byte)body.NrLangue.Value;
        var langueExists = await _db.Langues.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Idlangue == langueId, ct);
        if (langueExists == null) return BadRequest($"Langue {body.NrLangue} inconnue.");

        var existing = await _db.LangueSources.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Tolkcode == tolkcode && x.NrLangue == body.NrLangue, ct);
        if (existing != null) return Conflict("Cette langue source existe déjà pour cet interprète.");

        var ent = new LangueSource
        {
            IdLanguesource = await GetNextValAsync("NR_AUTO_LANGUE_SOURCE", ct), // <-- forcer NEXTVAL
            Tolkcode = tolkcode,
            NrLangue = body.NrLangue,
            TaalcodeOld = null
        };

        _db.LangueSources.Add(ent);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSources), new { tolkcode }, new { id = ent.IdLanguesource });
    }

    // DELETE /api/interpretes/{tolkcode}/langues/source/{id}
    [HttpDelete("interpretes/{tolkcode:int}/langues/source/{id:int}")]
    public async Task<IActionResult> RemoveSource(int tolkcode, int id, CancellationToken ct)
    {
        var ent = await _db.LangueSources
            .FirstOrDefaultAsync(x => x.IdLanguesource == id && x.Tolkcode == tolkcode, ct);
        if (ent is null) return NotFound();

        _db.LangueSources.Remove(ent);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------------- Langues DESTINATION par interprète ----------------
    // GET /api/interpretes/{tolkcode}/langues/destination
    [HttpGet("interpretes/{tolkcode:int}/langues/destination")]
    public async Task<ActionResult<IEnumerable<LangueDestinationDto>>> GetDestinations(int tolkcode, CancellationToken ct)
    {
        var q =
            from ld in _db.LangueDestinations.AsNoTracking().Where(x => x.Tolkcode == tolkcode)
            join l in _db.Langues.AsNoTracking() on (int)ld.NrLangue equals (int)l.Idlangue
            orderby l.LibelleFr
            select new LangueDestinationDto
            {
                IdLanguedestination = ld.IdLanguedestination,
                Tolkcode = ld.Tolkcode ?? tolkcode,
                NrLangue = ld.NrLangue,
                CodeIso = l.CodeIso,
                LibelleFr = l.LibelleFr,
                LibelleNl = l.LibelleNl
            };

        return Ok(await q.ToListAsync(ct));
    }

    // POST /api/interpretes/{tolkcode}/langues/destination
    [HttpPost("interpretes/{tolkcode:int}/langues/destination")]
    public async Task<IActionResult> AddDestination(int tolkcode, [FromBody] AddLangueDto body, CancellationToken ct)
    {
        if (body is null || body.NrLangue is null || body.NrLangue <= 0)
            return BadRequest("NrLangue requis.");

        // FIX: Utiliser FirstOrDefaultAsync au lieu de AnyAsync pour éviter les problèmes Oracle
        var tolkExists = await _db.Tolkidentities.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tolkcode == tolkcode, ct);
        if (tolkExists == null) return NotFound($"Interprète {tolkcode} introuvable.");

        var langueId = (byte)body.NrLangue.Value;
        var langueExists = await _db.Langues.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Idlangue == langueId, ct);
        if (langueExists == null) return BadRequest($"Langue {body.NrLangue} inconnue.");

        var existing = await _db.LangueDestinations.AsNoTracking()
            .FirstOrDefaultAsync(x => (x.Tolkcode ?? tolkcode) == tolkcode && x.NrLangue == body.NrLangue, ct);
        if (existing != null) return Conflict("Cette langue destination existe déjà pour cet interprète.");

        var ent = new LangueDestination
        {
            IdLanguedestination = await GetNextValAsync("NR_AUTO_DESTINATION", ct), // <-- forcer NEXTVAL
            Tolkcode = tolkcode,
            NrLangue = body.NrLangue.Value
        };

        _db.LangueDestinations.Add(ent);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetDestinations), new { tolkcode }, new { id = ent.IdLanguedestination });
    }

    // DELETE /api/interpretes/{tolkcode:int}/langues/destination/{id}
    [HttpDelete("interpretes/{tolkcode:int}/langues/destination/{id:int}")]
    public async Task<IActionResult> RemoveDestination(int tolkcode, int id, CancellationToken ct)
    {
        var ent = await _db.LangueDestinations
            .FirstOrDefaultAsync(x => x.IdLanguedestination == id && (x.Tolkcode ?? tolkcode) == tolkcode, ct);
        if (ent is null) return NotFound();

        _db.LangueDestinations.Remove(ent);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------------- Utils ----------------
    private async Task<int> GetNextValAsync(string sequenceName, CancellationToken ct)
    {
        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            using var cmd = _db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = $"SELECT {sequenceName}.NEXTVAL FROM DUAL";
            cmd.CommandType = CommandType.Text;
            var scalar = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(scalar);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }
}

// ---------------- DTOs ----------------
public record LangueDto
{
    public byte? Idlangue { get; init; }
    public string? CodeIso { get; init; }
    public string? LibelleFr { get; init; }
    public string? LibelleNl { get; init; }
}

public record LangueSourceDto
{
    public int IdLangueSource { get; init; }
    public int Tolkcode { get; init; }
    public int NrLangue { get; init; }
    public string? LibelleFr { get; init; }
    public string? LibelleNl { get; init; }
}

public record LangueDestinationDto
{
    public int IdLanguedestination { get; init; }
    public int Tolkcode { get; init; }
    public int NrLangue { get; init; }
    public string? CodeIso { get; init; }
    public string? LibelleFr { get; init; }
    public string? LibelleNl { get; init; }
}

public record AddLangueDto
{
    public int? NrLangue { get; init; } // correspond à LANGUE.IDLANGUE
}