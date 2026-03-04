using AutoMapper;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/interpretes/{tolkcode:int}/tolklink")]
[Produces("application/json")]
public class TolklinkController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    public TolklinkController(ApplicationDbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    // POST /api/interpretes/{tolkcode}/tolklink  -> ajoute 1 lien
    [HttpPost]
    public async Task<IActionResult> Add(int tolkcode, [FromBody] NewTolklinkDto dto)
    {
        if (dto == null || dto.NrAffAudience <= 0)
            return BadRequest("NrAffAudience invalide.");

        // Oracle-safe: COUNT(*) au lieu de Any(predicate)
        var count = await _db.Tolklinks.AsNoTracking()
            .Where(x => x.Tolkcode == tolkcode
                        && x.NrAffAudience == dto.NrAffAudience
                        && x.Datesupp == null)
            .Select(x => x.IdTolklink)
            .Take(1)
            .CountAsync();

        if (count > 0)
            return Conflict("Lien déjà existant.");

        var row = new Tolklink
        {
            Tolkcode = tolkcode,
            NrAffAudience = dto.NrAffAudience,
            Datecreate = DateTime.Now,
            Usercreate = "api"
        };

        _db.Tolklinks.Add(row);
        await _db.SaveChangesAsync();
        return Ok(new { id = row.IdTolklink });
    }

    // DELETE /api/interpretes/{tolkcode}/tolklink/{idAffAudience}  -> soft-delete (DATE_SUPP)
    [HttpDelete("{idAffAudience:int}")]
    public async Task<IActionResult> Remove(int tolkcode, int idAffAudience)
    {
        var row = await _db.Tolklinks
            .Where(x => x.Tolkcode == tolkcode
                        && x.NrAffAudience == idAffAudience
                        && x.Datesupp == null)
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound("Lien introuvable ou déjà supprimé.");

        row.Datesupp = DateTime.Now;
        row.Datemodif = DateTime.Now;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/interpretes/{tolkcode}/tolklink/bulk  -> ajoute plusieurs liens
    [HttpPost("bulk")]
    public async Task<IActionResult> AddBulk(int tolkcode, [FromBody] BulkNewTolklinkDto dto)
    {
        if (dto?.Ids == null || dto.Ids.Length == 0)
            return BadRequest("Liste vide.");

        var ids = dto.Ids.Distinct().ToArray();

        var already = await _db.Tolklinks.AsNoTracking()
            .Where(x => x.Tolkcode == tolkcode
                        && x.Datesupp == null
                        && x.NrAffAudience.HasValue
                        && ids.Contains((int)x.NrAffAudience.Value))
            .Select(x => x.NrAffAudience!.Value)
            .ToListAsync();

        var toInsert = ids.Except(already.Select(a => (int)a)).ToList();

        foreach (var idAff in toInsert)
        {
            _db.Tolklinks.Add(new Tolklink
            {
                Tolkcode = tolkcode,
                NrAffAudience = idAff,
                Datecreate = DateTime.Now,
                Usercreate = "api"
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { inserted = toInsert.Count, skipped = already.Count });
    }
}
