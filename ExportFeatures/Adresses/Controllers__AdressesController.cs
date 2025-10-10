using Dragoman.Server.Models;   // Tolkadresse, Tolkidentity
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class AdressesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdressesController(ApplicationDbContext db) => _db = db;

    // ---------------- Par interprète ----------------

    // GET /api/interpretes/{tolkcode}/adresses?onlyActive=true
    [HttpGet("interpretes/{tolkcode:int}/adresses")]
    public async Task<IActionResult> ListByTolk(int tolkcode, [FromQuery] bool onlyActive = false)
    {
        // Dans TOLKADRESSE, TOLKCODE est stocké en VARCHAR2 => on compare en string
        string sCode = tolkcode.ToString();

        var q = _db.Tolkadresses.AsNoTracking()
                .Where(a => a.Tolkcode == sCode);

        if (onlyActive)
            q = q.Where(a => a.Enddate == null); // adresse “active” = ENDDATE NULL

        var rows = await q
            .OrderByDescending(a => a.Startdate)
            .ToListAsync();

        return Ok(rows);
    }

    // POST /api/interpretes/{tolkcode}/adresses
    // Corps = entité Tolkadresse (pas de DTO)
    [HttpPost("interpretes/{tolkcode:int}/adresses")]
    public async Task<IActionResult> Create(int tolkcode, [FromBody] Tolkadresse body)
    {
        if (body is null) return BadRequest("Payload manquant.");

        // L’interprète existe ?
        bool exists = await _db.Tolkidentities.AsNoTracking()
                            .AnyAsync(t => t.Tolkcode == tolkcode);
        if (!exists) return NotFound($"Interprète {tolkcode} introuvable.");

        // Forcer le rattachement
        body.Tolkcode = tolkcode.ToString();

        // Petites validations côté serveur (Oracle a LAND=2 chars)
        if (string.IsNullOrWhiteSpace(body.Land) || body.Land.Length > 2)
            return BadRequest("Le code pays (LAND) est requis et doit faire 2 caractères.");

        // Champs d’audit + défauts utiles
        var now = DateTime.UtcNow;
        if (body.Startdate == default) body.Startdate = now.Date;
        body.Datecreate = now;
        body.Usercreate = User?.Identity?.Name ?? "system";
        body.Datemodif = null;
        body.Usermodif = null;

        // Ne PAS affecter IdAdresse : la séquence Oracle (NR_AUTO_ADRESSE.NEXTVAL) s’en charge
        _db.Tolkadresses.Add(body);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { id = body.IdAdresse }, body);
    }

    // POST /api/interpretes/{tolkcode}/adresses/replace
    // Clôture l’adresse active (ENDDATE) puis crée la nouvelle (ENDDATE NULL)
    [HttpPost("interpretes/{tolkcode:int}/adresses/replace")]
    public async Task<IActionResult> ReplaceOrCreate(int tolkcode, [FromBody] Tolkadresse body)
    {
        if (body is null) return BadRequest("Payload manquant.");
        if (body.Startdate == default) return BadRequest("StartDate est requis.");

        // L’interprète existe ?
        bool exists = await _db.Tolkidentities.AsNoTracking()
                            .AnyAsync(t => t.Tolkcode == tolkcode);
        if (!exists) return NotFound($"Interprète {tolkcode} introuvable.");

        // Validation LAND 2 chars (évite ORA-12899)
        if (string.IsNullOrWhiteSpace(body.Land) || body.Land.Length > 2)
            return BadRequest("Le code pays (LAND) est requis et doit faire 2 caractères.");

        var sCode = tolkcode.ToString();
        var now = DateTime.UtcNow;

        using var tx = await _db.Database.BeginTransactionAsync();

        // Adresse active actuelle (ENDDATE NULL)
        var active = await _db.Tolkadresses
            .Where(a => a.Tolkcode == sCode && a.Enddate == null)
            .OrderByDescending(a => a.Startdate)
            .FirstOrDefaultAsync();

        if (active != null)
        {
            // On clôture l’ancienne la veille du début de la nouvelle (ou le jour même selon ta règle)
            active.Enddate = body.Startdate.Date.AddDays(-1);
            active.Datemodif = now;
            active.Usermodif = User?.Identity?.Name ?? "system";

            await _db.SaveChangesAsync();
        }

        // Créer la nouvelle adresse (ENDDATE = NULL)
        var newAdr = new Tolkadresse
        {
            Tolkcode = sCode,
            Land = body.Land,
            Cp = body.Cp,
            Commune = body.Commune,
            Rue = body.Rue,
            Numero = body.Numero,
            Boite = body.Boite,
            Km = body.Km,
            Startdate = body.Startdate.Date,
            Enddate = null,
            Datecreate = now,
            Usercreate = User?.Identity?.Name ?? "system",
            Datemodif = null,
            Usermodif = null
        };

        _db.Tolkadresses.Add(newAdr);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return CreatedAtAction(nameof(GetOne), new { id = newAdr.IdAdresse }, newAdr);
    }

    // ---------------- Par adresse ----------------

    // GET /api/adresses/{id}
    [HttpGet("adresses/{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var ent = await _db.Tolkadresses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdAdresse == id);
        return ent is null ? NotFound() : Ok(ent);
    }

    // PUT /api/adresses/{id}
    // ⚠️ NE PAS réécrire Tolkcode (évite ORA-01407 “TOLKCODE cannot be updated to NULL”)
    [HttpPut("adresses/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Tolkadresse input)
    {
        if (input is null) return BadRequest("Payload manquant.");
        if (input.IdAdresse != 0 && input.IdAdresse != id)
            return BadRequest("Id incohérent.");

        var ent = await _db.Tolkadresses.FirstOrDefaultAsync(x => x.IdAdresse == id);
        if (ent is null) return NotFound();

        // Validation LAND 2 chars
        if (!string.IsNullOrWhiteSpace(input.Land) && input.Land.Length > 2)
            return BadRequest("Le code pays (LAND) doit faire 2 caractères.");

        // Met à jour uniquement les champs “éditables”
        ent.Land = input.Land;
        ent.Cp = input.Cp;
        ent.Commune = input.Commune;
        ent.Rue = input.Rue;
        ent.Numero = input.Numero;
        ent.Boite = input.Boite;
        ent.Km = input.Km;
        ent.Startdate = input.Startdate == default ? ent.Startdate : input.Startdate.Date;
        ent.Enddate = input.Enddate?.Date;
        // 🔒 NE PAS toucher à ent.Tolkcode ici

        ent.Datemodif = DateTime.UtcNow;
        ent.Usermodif = User?.Identity?.Name ?? "system";

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/adresses/{id}
    [HttpDelete("adresses/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ent = await _db.Tolkadresses.FirstOrDefaultAsync(x => x.IdAdresse == id);
        if (ent is null) return NotFound();

        _db.Tolkadresses.Remove(ent);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
