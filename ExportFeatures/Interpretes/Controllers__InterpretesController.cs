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

    // GET /api/interpretes/search?mode=tolkcode|nom|prenom|langue&q=...
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<InterpreteSearchDto>>> Search([FromQuery] string mode, [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(q))
            return BadRequest("Paramètres mode et q requis.");

        string s = q.Trim().ToUpperInvariant();

        var baseQ = _db.Tolkidentities.AsNoTracking();

        if (mode == "tolkcode")
        {
            baseQ = baseQ.Where(t => t.Tolkcode.ToString().Contains(s));
        }
        else if (mode == "nom")
        {
            baseQ = baseQ.Where(t => (t.Nom ?? "").ToUpper().Contains(s));
        }
        else if (mode == "prenom")
        {
            baseQ = baseQ.Where(t => (t.Prenom ?? "").ToUpper().Contains(s));
        }
        else if (mode == "langue")
        {
            // IDs de langues en INT pour éviter le byte/int mismatch
            var idsLangueInt = await _db.Langues
                .Where(l =>
                    l.LibelleFr.ToUpper().Contains(s) ||
                    ((l.LibelleNl ?? "").ToUpper().Contains(s)) ||
                    ((l.CodeIso ?? "").ToUpper().Contains(s)))
                .Select(l => (int)l.Idlangue)   // <-- forcer int ici
                .ToListAsync();

            baseQ = baseQ.Where(t =>
                _db.LangueDestinations.Any(ld =>
                    ld.Tolkcode == t.Tolkcode &&
                    idsLangueInt.Contains((int)ld.NrLangue)   // <-- comparer en int
                )
                ||
                _db.LangueSources.Any(ls =>
                    ls.Tolkcode == t.Tolkcode &&
                    idsLangueInt.Contains((int)ls.NrLangue)   // <-- comparer en int
                )
            );
        }
        else
        {
            return BadRequest("mode invalide.");
        }

        var rows = await baseQ
            .OrderBy(t => t.Tolkcode)
            .Take(200)
            .Select(t => new InterpreteSearchDto
            {
                Tolkcode = t.Tolkcode.ToString(),
                Nom = t.Nom,
                Prenom = t.Prenom,
                LanguesDestination = _db.LangueDestinations
                    .Where(ld => ld.Tolkcode == t.Tolkcode)
                    .Join(_db.Langues,
                          ld => (int)ld.NrLangue,
                          l => (int)l.Idlangue,           // <-- forcer int des deux côtés
                          (ld, l) => l.LibelleFr)
                    .Take(10).ToList(),
                LanguesSource = _db.LangueSources
                    .Where(ls => ls.Tolkcode == t.Tolkcode)
                    .Join(_db.Langues,
                          ls => (int)ls.NrLangue,
                          l => (int)l.Idlangue,           // <-- idem
                          (ls, l) => l.LibelleFr)
                    .Take(10).ToList()
            })
            .ToListAsync();

        return Ok(rows);
    }

    // GET /api/interpretes/{tolkcode}/audiences-exact
    [HttpGet("{tolkcode:int}/audiences-exact")]
    public async Task<ActionResult<IEnumerable<AudienceDto>>> AudiencesExactes(int tolkcode)
    {
        var today = DateTime.Today;
        const int FR = 36, NL = 77;

        var vrm = _db.VueCalendarVrmPc
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

        var ann = _db.VueCalendarAnn
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

        var aud = vrm.Concat(ann);

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
                SrcId = (int)lsrc.Idlangue  // <-- forcer int ici aussi
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
}
