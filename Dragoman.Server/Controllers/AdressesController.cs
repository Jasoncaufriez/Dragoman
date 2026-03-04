using Dragoman.Server.Models;   // Tolkadresse, Tolkidentity
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class AdressesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdressesController(ApplicationDbContext db) => _db = db;

    // Oracle: si pas de trigger/identity sur ID_ADRESSE, on prend la séquence à la main.
    private async Task<decimal> NextIdAdresseAsync()
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT NR_AUTO_ADRESSE.NEXTVAL FROM DUAL";
        var val = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(val);
    }

    // GET /api/interpretes/{tolkcode}/adresses?onlyActive=true
    [HttpGet("interpretes/{tolkcode:int}/adresses")]
    public async Task<IActionResult> ListByTolk(int tolkcode, [FromQuery] bool onlyActive = false)
    {
        string sCode = tolkcode.ToString();

        var q = _db.Tolkadresses.AsNoTracking()
            .Where(a => a.Tolkcode == sCode);

        if (onlyActive)
            q = q.Where(a => a.Enddate == null);

        var rows = await q
            .OrderByDescending(a => a.Startdate)
            .ToListAsync();

        return Ok(rows);
    }

    // POST /api/interpretes/{tolkcode}/adresses
    [HttpPost("interpretes/{tolkcode:int}/adresses")]
    public async Task<IActionResult> Create(int tolkcode, [FromBody] Tolkadresse body)
    {
        if (body is null) return BadRequest("Payload manquant.");

        // Oracle: éviter AnyAsync() -> TRUE/FALSE
        bool exists = (await _db.Tolkidentities.AsNoTracking()
            .CountAsync(t => t.Tolkcode == tolkcode)) > 0;

        if (!exists) return NotFound($"Interprète {tolkcode} introuvable.");

        body.Tolkcode = tolkcode.ToString();

        // LAND = 2 chars (normalise)
        body.Land = (body.Land ?? "").Trim().ToUpperInvariant();
        if (body.Land.Length != 2)
            return BadRequest("Le code pays (LAND) est requis et doit faire 2 caractères.");

        var now = DateTime.UtcNow;
        if (body.Startdate == default) body.Startdate = now.Date;
        body.Datecreate = now;
        body.Usercreate = User?.Identity?.Name ?? "system";
        body.Datemodif = null;
        body.Usermodif = null;

        // IMPORTANT: générer ID_ADRESSE si DB ne le fait pas
        // (adapte le test selon le type réel : decimal/int)
        if (body.IdAdresse == null || Convert.ToDecimal(body.IdAdresse) == 0)
            body.IdAdresse = (int)await NextIdAdresseAsync();

        _db.Tolkadresses.Add(body);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { id = body.IdAdresse }, body);
    }

    // POST /api/interpretes/{tolkcode}/adresses/replace
    [HttpPost("interpretes/{tolkcode:int}/adresses/replace")]
    public async Task<IActionResult> ReplaceOrCreate(int tolkcode, [FromBody] Tolkadresse body)
    {
        if (body is null) return BadRequest("Payload manquant.");
        if (body.Startdate == default) return BadRequest("StartDate est requis.");

        // Oracle: éviter AnyAsync() -> TRUE/FALSE
        bool exists = (await _db.Tolkidentities.AsNoTracking()
            .CountAsync(t => t.Tolkcode == tolkcode)) > 0;

        if (!exists) return NotFound($"Interprète {tolkcode} introuvable.");

        body.Land = (body.Land ?? "").Trim().ToUpperInvariant();
        if (body.Land.Length != 2)
            return BadRequest("Le code pays (LAND) est requis et doit faire 2 caractères.");

        var sCode = tolkcode.ToString();
        var now = DateTime.UtcNow;

        using var tx = await _db.Database.BeginTransactionAsync();

        var active = await _db.Tolkadresses
            .Where(a => a.Tolkcode == sCode && a.Enddate == null)
            .OrderByDescending(a => a.Startdate)
            .FirstOrDefaultAsync();

        if (active != null)
        {
            active.Enddate = body.Startdate.Date.AddDays(-1);
            active.Datemodif = now;
            active.Usermodif = User?.Identity?.Name ?? "system";
            await _db.SaveChangesAsync();
        }

        var newAdr = new Tolkadresse
        {
            // ID obligatoire -> séquence
            IdAdresse = (int)await NextIdAdresseAsync(),

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

    // GET /api/adresses/{id}
    [HttpGet("adresses/{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var ent = await _db.Tolkadresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdAdresse == id);
        return ent is null ? NotFound() : Ok(ent);
    }

    // PUT /api/adresses/{id}
    [HttpPut("adresses/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Tolkadresse input)
    {
        if (input is null) return BadRequest("Payload manquant.");

        var ent = await _db.Tolkadresses.FirstOrDefaultAsync(x => x.IdAdresse == id);
        if (ent is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(input.Land))
        {
            var land = input.Land.Trim().ToUpperInvariant();
            if (land.Length != 2) return BadRequest("Le code pays (LAND) doit faire 2 caractères.");
            ent.Land = land;
        }

        ent.Cp = input.Cp;
        ent.Commune = input.Commune;
        ent.Rue = input.Rue;
        ent.Numero = input.Numero;
        ent.Boite = input.Boite;
        ent.Km = input.Km;
        ent.Startdate = input.Startdate == default ? ent.Startdate : input.Startdate.Date;
        ent.Enddate = input.Enddate?.Date;

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
