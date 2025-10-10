using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api/prestations")]
[Produces("application/json")]
public class PrestationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PrestationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET api/prestations/jour?date=2025-10-02
    [HttpGet("jour")]
    public async Task<ActionResult<IEnumerable<PrestationJourRowDto>>> GetJour(
        [FromQuery] DateTime date,
        CancellationToken ct = default)
    {
        var d0 = date.Date;
        var d1 = d0.AddDays(1);

        // ---- VRM_PC : inner join (pas de comparaison d'entités keyless)
        // Important : on force IdAffAudience non null côté vue pour pouvoir caster .Value ensuite
        var linksQuery =
            from tl in _db.Tolklinks.AsNoTracking()
            where tl.Datesupp == null
            join vrm in _db.VueCalendarVrmPcs
                .Where(v => v.IdAffAudience != 0 && v.DateAudience >= d0 && v.DateAudience < d1)
                on tl.NrAffAudience equals (int)vrm.IdAffAudience!
            select new
            {
                tl.Tolkcode,
                tl.NrAffAudience,
                tl.IdPrestation,
                HeureAudience = vrm.HeureAudience
            };

        // ---- ANN : inner join idem
        var linksAnnQuery =
            from tl in _db.Tolklinks.AsNoTracking()
            where tl.Datesupp == null
            join ann in _db.VueCalendarAnns
                .Where(a => a.IdAffAudience != 0 && a.DateAudience >= d0 && a.DateAudience < d1)
                on tl.NrAffAudience equals (int)ann.IdAffAudience
            select new
            {
                tl.Tolkcode,
                tl.NrAffAudience,
                tl.IdPrestation,
                HeureAudience = ann.HeureAudience
            };

        var links = await linksQuery.Union(linksAnnQuery).ToListAsync(ct);

        // Grouper par interprète
        var grouped = links
            .GroupBy(x => x.Tolkcode) // Tolkcode est string côté TOLKLINK
            .Select(g => new
            {
                Tolkcode = g.Key,
                IdAffAudiences = g.Select(z => (int)z.NrAffAudience).Distinct().ToArray(),
                HasPrestation = g.Any(z => z.IdPrestation.HasValue),
                MinHeure = g.Where(z => z.HeureAudience != null).Min(z => z.HeureAudience)
            })
            .ToList();

        // Charger infos interprètes (Tolkidentity.Tolkcode = int)
        var tolkcodesInt = grouped
            .Select(g => g.Tolkcode != null && decimal.TryParse(g.Tolkcode.ToString(), out var tc) ? (int)tc : 0)
            .Where(tc => tc > 0)
            .ToList();

        var interpretes = await _db.Tolkidentities.AsNoTracking()
            .Where(t => tolkcodesInt.Contains(t.Tolkcode))
            .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        var result = grouped
            .Select(g =>
            {
                var key = g.Tolkcode.HasValue ? g.Tolkcode.Value.ToString() : "0";
                interpretes.TryGetValue(key, out var interp);
                return new PrestationJourRowDto
                {
                    Tolkcode = (g.Tolkcode.HasValue ? g.Tolkcode.Value.ToString() : "0"),
                    Nom = interp?.Nom ?? "",
                    Prenom = interp?.Prenom ?? "",
                    Telephone = string.Join(" / ", new[] { interp?.Gsm, interp?.Tel, interp?.Telbis }
                        .Where(s => !string.IsNullOrWhiteSpace(s))),
                    IdAffAudiences = g.IdAffAudiences,
                    HeureAudienceSuggee = g.MinHeure,
                    HasPrestation = g.HasPrestation
                };
            })
            .OrderBy(r => r.Nom)
            .ThenBy(r => r.Prenom)
            .ToList();

        return Ok(result);
    }

    // POST api/prestations
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewPrestationDto dto, CancellationToken ct = default)
    {
        if (dto.Endheure <= dto.Startheure)
            return BadRequest("L'heure de fin doit être postérieure à l'heure de début.");

        // Convertir le tolkcode (string -> int) HORS EF pour éviter les casts dans la requête
        if (!int.TryParse(dto.Tolkcode, out var tolkcodeInt))
            return NotFound($"Interprète {dto.Tolkcode} introuvable.");

        var tolkExists = await _db.Tolkidentities.AsNoTracking()
            .AnyAsync(t => t.Tolkcode == tolkcodeInt, ct);
        if (!tolkExists)
            return NotFound($"Interprète {dto.Tolkcode} introuvable.");

        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 1) Paiement minimal
            var paiement = new Paiement
            {
                Tolkcode = dto.Tolkcode,                // stocké tel quel (string) dans ta table
                DatePrestation = dto.DatePrestation.Date,
                Montant = 0,
                Transport = 0,
                Total = 0,
                MontantTva = 0
            };
            _db.Paiements.Add(paiement);
            await _db.SaveChangesAsync(ct);

            // 2) Prestation
            var prestation = new Prestation
            {
                Tolkcode = dto.Tolkcode,
                DatePrestation = dto.DatePrestation.Date,
                Startheure = dto.Startheure,
                Endheure = dto.Endheure,
                UserCreate = User?.Identity?.Name ?? "api",
                IdPaiement = paiement.IdPaiement
            };
            _db.Prestations.Add(prestation);
            await _db.SaveChangesAsync(ct);

            // 3) Lier les TOLKLINK
            if (dto.IdAffAudiences?.Length > 0)
            {
                // Utilise List<int> pour Contains EF-friendly
                var ids = dto.IdAffAudiences.Distinct().ToList();

                var linksToUpdate = await _db.Tolklinks
                    .Where(x => x.Tolkcode.ToString() == dto.Tolkcode
                             && ids.Contains((int)x.NrAffAudience)
                             && x.Datesupp == null)
                    .ToListAsync(ct);

                foreach (var link in linksToUpdate)
                    link.IdPrestation = prestation.IdPrestation;

                await _db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    // POST api/prestations/absence
    [HttpPost("absence")]
    public async Task<IActionResult> MarquerAbsent([FromBody] AbsenceDto dto, CancellationToken ct = default)
    {
        var link = await _db.Tolklinks
            .FirstOrDefaultAsync(x => x.Tolkcode.HasValue && x.Tolkcode.Value.ToString() == dto.Tolkcode
                                   && x.NrAffAudience == dto.IdAffAudience
                                   && x.Datesupp == null, ct);

        if (link == null)
            return NotFound("Lien interprète-audience introuvable ou déjà supprimé.");
    
        link.Datesupp = dto.DatePrestation.Date;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST api/prestations/remplacement
    [HttpPost("remplacement")]
    public async Task<IActionResult> Remplacer([FromBody] RemplacementDto dto, CancellationToken ct = default)
    {
        if (!int.TryParse(dto.NouveauTolkcode, out var nouveauInt))
            return NotFound($"Nouvel interprète {dto.NouveauTolkcode} introuvable.");

        var nouveauExists = await _db.Tolkidentities.AsNoTracking()
            .AnyAsync(t => t.Tolkcode == nouveauInt, ct);
        if (!nouveauExists)
            return NotFound($"Nouvel interprète {dto.NouveauTolkcode} introuvable.");

        var link = await _db.Tolklinks
            .FirstOrDefaultAsync(x => x.Tolkcode.HasValue && x.Tolkcode.Value.ToString() == dto.AncienTolkcode
                                   && x.NrAffAudience == dto.IdAffAudience
                                   && x.Datesupp == null, ct);

        if (link == null)
            return NotFound("Lien interprète-audience introuvable ou déjà supprimé.");

        link.Tolkcode = decimal.Parse(dto.NouveauTolkcode);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
