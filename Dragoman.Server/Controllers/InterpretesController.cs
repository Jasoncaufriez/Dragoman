using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api/interpretes")]
[Produces("application/json")]
public class InterpretesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InterpretesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/interpretes/1055
    [HttpGet("{tolkcode:int}")]
    public async Task<IActionResult> Get(int tolkcode)
    {
        var ent = await _db.Tolkidentities.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tolkcode == tolkcode);
        return ent is null ? NotFound() : Ok(ent);
    }

    // GET /api/interpretes?take=50&skip=0
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (skip < 0) skip = 0;
        take = Math.Clamp(take, 1, 200);

        var items = await _db.Tolkidentities.AsNoTracking()
            .OrderBy(t => t.Tolkcode)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(items);
    }

    // POST /api/interpretes
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Tolkidentity input)
    {
        if (input is null) return BadRequest("Payload manquant.");

        // Normalisations basiques
        input.Beedigd = Normalize01(input.Beedigd);         // 0/1
        input.Taalrol = NormalizeTaalrol(input.Taalrol);    // 1=FR,2=NL sinon null

        _db.Tolkidentities.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { tolkcode = input.Tolkcode }, input);
    }

    // PUT /api/interpretes/{tolkcode}
    [HttpPut("{tolkcode:int}")]
    public async Task<IActionResult> Put(int tolkcode, [FromBody] Tolkidentity input)
    {
        if (input is null || input.Tolkcode != tolkcode)
            return BadRequest("Tolkcode incohérent.");

        var ent = await _db.Tolkidentities.FirstOrDefaultAsync(t => t.Tolkcode == tolkcode);
        if (ent is null) return NotFound();

        // Champs autorisés
        ent.Nom = input.Nom;
        ent.Prenom = input.Prenom;
        ent.Email = input.Email;
        ent.Fax = input.Fax;

        ent.Taalrol = NormalizeTaalrol(input.Taalrol);
        ent.Beedigd = Normalize01(input.Beedigd);

        ent.DateNaissance = input.DateNaissance;
        ent.Nationaliteit = input.Nationaliteit;
        ent.Rijksregisternr = input.Rijksregisternr;
        ent.Herkomst = input.Herkomst;
        ent.Genre = input.Genre;
        ent.Beroepscode = input.Beroepscode;

        ent.BtwNr = input.BtwNr;
        ent.Bankrekening = input.Bankrekening;
        ent.Iban = input.Iban;
        ent.Tva = input.Tva;

        ent.Remarque = input.Remarque;
        ent.Evaluatiecode = input.Evaluatiecode;
        ent.Ba = input.Ba;
        ent.Fedcom = input.Fedcom;
        ent.Ondernemingsnummer = input.Ondernemingsnummer;
        ent.Vestigingsnummer = input.Vestigingsnummer;
        ent.Fedcomnummer = input.Fedcomnummer;
        ent.Iscce = input.Iscce;

        ent.Gsm = input.Gsm;
        ent.Tel = input.Tel;
        ent.Telbis = input.Telbis;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/interpretes/{tolkcode}
    [HttpDelete("{tolkcode:int}")]
    public async Task<IActionResult> Delete(int tolkcode)
    {
        var ent = await _db.Tolkidentities.FirstOrDefaultAsync(t => t.Tolkcode == tolkcode);
        if (ent is null) return NotFound();

        _db.Tolkidentities.Remove(ent);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/interpretes/match?langSrc=6&langDst=36&date=2025-09-19
    [HttpGet("match")]
    public async Task<ActionResult<IEnumerable<InterpreteMatchDto>>> Match(
        [FromQuery] int langSrc,
        [FromQuery] int langDst,
        [FromQuery] DateOnly date,
        CancellationToken ct = default)
    {
        if (langSrc <= 0 || langDst <= 0) return BadRequest("langSrc et langDst requis.");

        var day = date.ToDateTime(new TimeOnly(0, 0));

        // 1) Candidats: ont la paire langues source -> destination et NE sont PAS indisponibles ce jour-là
        var candidatsQ =
            from t in _db.Tolkidentities.AsNoTracking()
            where _db.LangueSources.Any(ls => ls.Tolkcode == t.Tolkcode && ls.NrLangue == langSrc)
               && _db.LangueDestinations.Any(ld => ld.Tolkcode == t.Tolkcode && ld.NrLangue == langDst)
               && !_db.Tolkindispos.Any(ind =>
                    ind.Tolkcode == t.Tolkcode.ToString() &&
                    ind.Startindispo <= day &&
                    (ind.Endindispo == null || ind.Endindispo > day))
            select new
            {
                t.Tolkcode,
                t.Nom,
                t.Prenom,
                t.Tel,
                t.Telbis,
                t.Gsm
            };

        var candidats = await candidatsQ.ToListAsync(ct);

        if (candidats.Count == 0)
            return Ok(Array.Empty<InterpreteMatchDto>());

        var tolkcodes = candidats.Select(c => c.Tolkcode).ToList();

        // 2) Distance (KM) depuis l'adresse active: ENDDATE IS NULL
        var kmByTolk = await _db.Tolkadresses.AsNoTracking()
            .Where(a => a.Enddate == null)
            .ToListAsync(ct);

        var kmMap = kmByTolk
            .Where(a => int.TryParse(a.Tolkcode, out var code) && tolkcodes.Contains(code))
            .GroupBy(a => int.Parse(a.Tolkcode))
            .ToDictionary(g => g.Key, g => g.First().Km);

        // 3) Libellés langues destination
        var languesByTolk = await _db.LangueDestinations.AsNoTracking()
            .Where(ld => tolkcodes.Contains(ld.Tolkcode ?? 0))
            .Join(_db.Langues.AsNoTracking(),
                  ld => ld.NrLangue,
                  l => l.Idlangue,
                  (ld, l) => new { Tolkcode = ld.Tolkcode ?? 0, Lib = l.LibelleFr })
            .ToListAsync(ct);

        var langsMap = languesByTolk
            .GroupBy(x => x.Tolkcode)
            .ToDictionary(g => g.Key, g => g.Select(z => z.Lib).Distinct().ToList());

        // 4) Assemblage en mémoire + tri distance
        var rows = candidats.Select(c => new InterpreteMatchDto
        {
            Tolkcode = c.Tolkcode,
            Nom = c.Nom,
            Prenom = c.Prenom,
            Tel = c.Tel,
            Telbis = c.Telbis,
            Gsm = c.Gsm,
            LanguesDestination = langsMap.TryGetValue(c.Tolkcode, out var ll) ? ll : new List<string>(),
            DistanceKm = kmMap.TryGetValue(c.Tolkcode, out var km) ? km : (double?)null
        })
        .OrderBy(r => r.DistanceKm ?? double.MaxValue)
        .ThenBy(r => r.Nom)
        .ThenBy(r => r.Prenom)
        .Take(300)
        .ToList();

        return Ok(rows);
    }

    // GET /api/interpretes/{tolkcode}/audiences-exact
    [HttpGet("{tolkcode:int}/audiences-exact")]
    public async Task<ActionResult<IEnumerable<AudienceDto>>> AudiencesExactes(int tolkcode)
    {
        var today = DateTime.Today;
        const int FR = 36, NL = 77;
        
        var vrm = _db.VueCalendarVrmPcs
            .Where(v => v.DateAudience.HasValue && v.DateAudience.Value >= today && v.Tolkcode == null)
            .Select(v => new
            {
                v.NroRoleGen,
                v.LangueRole,
                v.Proc,
                DateAudience = v.DateAudience.Value,
                v.Nom,
                v.SalleAudience,
                v.HeureAudience,
                v.LangueRequete,
                v.LibelleFr,
                v.LangueCgoe,
                v.IdAffAudience,
                v.Tolkcode
            });

        var ann = _db.VueCalendarAnns
            .Where(v => v.DateAudience.HasValue && v.DateAudience.Value >= today && v.Tolkcode == null)
            .Select(v => new
            {
                v.NroRoleGen,
                v.LangueRole,
                v.Proc,
                DateAudience = v.DateAudience.Value,
                v.Nom,
                v.SalleAudience,
                v.HeureAudience,
                v.LangueRequete,
                v.LibelleFr,
                v.LangueCgoe,
                v.IdAffAudience,
                v.Tolkcode
            });

        var aud = vrm;

        // Joindre la langue source exacte (depuis libellé -> IDLANGUE)
        var audWithSrcId =
            from a in aud
            join lsrc in _db.Langues on a.LangueRequete equals lsrc.LibelleFr
            select new
            {
                a.NroRoleGen,
                a.LangueRole,
                a.Proc,
                a.DateAudience,
                a.Nom,
                a.SalleAudience,
                a.HeureAudience,
                a.LangueRequete,
                a.LibelleFr,
                a.LangueCgoe,
                a.IdAffAudience,
                a.Tolkcode,
                SrcId = (int)lsrc.Idlangue
            };

        var rows = await (
            from a in audWithSrcId
            let roleId = (a.LangueRole == "F" ? FR : (a.LangueRole == "N" ? NL : 33))
            join ls in _db.LangueSources on tolkcode equals ls.Tolkcode
            join ld in _db.LangueDestinations on tolkcode equals ld.Tolkcode
            where (int)ls.NrLangue == a.SrcId && (int)ld.NrLangue == roleId
            where !_db.Tolkindispos
                .Where(i => i.Tolkcode == tolkcode.ToString())
                .Any(i =>
                    i.Startindispo <= a.DateAudience &&
                    (i.Endindispo == null || i.Endindispo >= a.DateAudience)
                )
            select new AudienceDto
            {
                NroRoleGen = a.NroRoleGen,
                LangueRole = a.LangueRole,
                Proc = a.Proc,
                DateAudience = a.DateAudience,
                Nom = a.Nom,
                SalleAudience = a.SalleAudience,
                HeureAudience = a.HeureAudience,
                LangueRequete = a.LangueRequete,
                LibelleFr = a.LibelleFr,
                LangueCgoe = a.LangueCgoe,
                IdAffAudience = a.IdAffAudience,
                Tolkcode = a.Tolkcode
            }
        )
        .OrderBy(r => r.DateAudience)
        .ThenBy(r => r.HeureAudience)
        .ToListAsync();

        return Ok(rows);
    }

    // --- helpers ---
    private static int? Normalize01(int? v) => v == 1 ? 1 : 0;
    private static int? NormalizeTaalrol(int? v) => (v is 1 or 2) ? v : null;
    private static int ConvertTolkcode(string tolkcode)
    {
        return int.TryParse(tolkcode, out var code) ? code : 0;
    }
}