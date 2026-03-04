using System;
using System.Collections.Generic;
using System.Data;
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

    // TVA par défaut (à adapter si besoin)
    private const decimal TVA_RATE = 0.21m;

    public PrestationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private async Task<int> NextValAsync(string sequenceName, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {sequenceName}.NEXTVAL FROM DUAL";
            var obj = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(obj);
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    [HttpGet("jour")]
    public async Task<ActionResult<IEnumerable<PrestationJourRowDto>>> GetJour(
        [FromQuery] DateOnly date,
        [FromQuery] bool includeAbsents = false,
        CancellationToken ct = default)
    {
        var d0 = date.ToDateTime(TimeOnly.MinValue);
        var d1 = d0.AddDays(1);


        // 1) Interprètes issus des vues calendrier (VRM/ANN) + TOLKLINK actifs ce jour
        // Si includeAbsents=true, on inclut aussi les liens avec Datesupp non null à cette date
        var linksQuery =
            from tl in _db.Tolklinks.AsNoTracking()
            where (includeAbsents
                    ? (tl.Datesupp == null || (tl.Datesupp >= d0 && tl.Datesupp < d1))
                    : (tl.Datesupp == null || tl.Datesupp >= d0))
            join vrm in _db.VueCalendarVrmPcs.AsNoTracking()
                .Where(v => v.IdAffAudience != 0 && v.DateAudience >= d0 && v.DateAudience < d1)
                on tl.NrAffAudience equals (int)vrm.IdAffAudience!
            select new
            {
                tl.Tolkcode,
                tl.NrAffAudience,
                tl.IdPrestation,
                tl.Datesupp,
                HeureAudience = vrm.HeureAudience
            };

        var linksAnnQuery =
            from tl in _db.Tolklinks.AsNoTracking()
            where (includeAbsents
                    ? (tl.Datesupp == null || (tl.Datesupp >= d0 && tl.Datesupp < d1))
                    : (tl.Datesupp == null || tl.Datesupp >= d0))
            join ann in _db.VueCalendarAnns.AsNoTracking()
                .Where(a => a.IdAffAudience != 0 && a.DateAudience >= d0 && a.DateAudience < d1)
                on tl.NrAffAudience equals (int)ann.IdAffAudience
            select new
            {
                tl.Tolkcode,
                tl.NrAffAudience,
                tl.IdPrestation,
                tl.Datesupp,
                HeureAudience = ann.HeureAudience
            };

        var links = await linksQuery.Union(linksAnnQuery).ToListAsync(ct);

        // Correction : HasPrestation = il existe une IdPrestation (peu importe l'état de la facture)
        var groupedCalendars = links
            .GroupBy(x => x.Tolkcode)
            .Select(g => new
            {
                TolkcodeInt = g.Key,
                IdAffAudiences = g.Where(z => z.NrAffAudience.HasValue)
                                  .Select(z => z.NrAffAudience!.Value)
                                  .Distinct()
                                  .ToArray(),
                HasPrestation = g.Any(z => z.IdPrestation.HasValue),
                Prestations = g.Where(z => z.IdPrestation.HasValue)
                               .Select(z => z.IdPrestation.Value)
                               .Distinct()
                               .ToArray(),
                MinHeure = g.Where(z => z.HeureAudience != null)
                            .Select(z => z.HeureAudience)
                            .DefaultIfEmpty(null)
                            .Min(),
                IsAbsent = includeAbsents && g.Any(z => z.Datesupp != null && z.Datesupp >= d0 && z.Datesupp < d1)
            })
            .ToList();

        // 2) Fallback: Prestations existantes ce jour (si les vues calendrier sont vides pour l’historique)
        var prestationsJour = await _db.Prestations.AsNoTracking()
            .Where(p => p.DatePrestation >= d0 && p.DatePrestation < d1)
            .Select(p => new { p.IdPrestation, p.Tolkcode, p.Startheure })
            .ToListAsync(ct);

        var prestationIds = prestationsJour.Select(p => p.IdPrestation).Distinct().ToList();

        var linksByPrestation = prestationIds.Count == 0
            ? new List<(int? IdPrestation, int? NrAffAudience)>()
            : await _db.Tolklinks.AsNoTracking()
                .Where(tl => tl.IdPrestation.HasValue && prestationIds.Contains(tl.IdPrestation.Value))
                .Select(tl => new { tl.IdPrestation, tl.NrAffAudience })
                .ToListAsync(ct)
                .ContinueWith(t => t.Result
                    .Select(x => (x.IdPrestation, x.NrAffAudience))
                    .ToList(), ct);

        var groupedPrestations = prestationsJour
            .GroupBy(p => p.Tolkcode)
            .Select(g =>
            {
                var idsPrest = g.Select(x => x.IdPrestation).Distinct().ToArray();
                var affs = linksByPrestation
                    .Where(x => x.IdPrestation.HasValue && idsPrest.Contains(x.IdPrestation.Value) && x.NrAffAudience.HasValue)
                    .Select(x => x.NrAffAudience!.Value)
                    .Distinct()
                    .ToArray();

                // heure suggérée = plus tôt Startheure du jour formatée "HH:mm"
                var minStart = g.Min(x => x.Startheure);
                var hhmm = minStart.ToString("HH:mm");

                return new
                {
                    TolkcodeStr = g.Key, // string dans PRESTATION
                    IdAffAudiences = affs,
                    HasPrestation = true,
                    MinHeure = hhmm
                };
            })
            .ToList();

        // 3) Fusion calendrier + fallback prestations (sans doublons)
        // Index existants par tolkcode string
        var calendarByTolkStr = groupedCalendars
            .Select(gc => new
            {
                Key = (gc.TolkcodeInt ?? 0).ToString(),
                gc.IdAffAudiences,
                gc.HasPrestation,
                gc.MinHeure,
                gc.IsAbsent,
                gc.Prestations
            })
            .ToDictionary(x => x.Key, x => x);

        if (calendarByTolkStr.Count == 0)
            return Ok(Array.Empty<PrestationJourRowDto>());

        // 4) Récup identités
        var allTolkStr = calendarByTolkStr.Keys.ToList();
        var tolkcodesInt = allTolkStr
            .Select(s => int.TryParse(s, out var i) ? i : 0)
            .Where(i => i > 0)
            .Distinct()
            .ToList();

        var interpretes = tolkcodesInt.Count == 0
            ? new Dictionary<string, Tolkidentity>()
            : await _db.Tolkidentities.AsNoTracking()
                  .Where(t => tolkcodesInt.Contains(t.Tolkcode))
                  .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        var result = calendarByTolkStr.Values
            .Select(x =>
            {
                interpretes.TryGetValue(x.Key, out var interp);
                return new PrestationJourRowDto
                {
                    Tolkcode = x.Key,
                    Nom = interp?.Nom ?? "",
                    Prenom = interp?.Prenom ?? "",
                    Telephone = string.Join(" / ", new[] { interp?.Gsm, interp?.Tel, interp?.Telbis }
                        .Where(s => !string.IsNullOrWhiteSpace(s))),
                    IdAffAudiences = x.IdAffAudiences,
                    HeureAudienceSuggee = x.MinHeure,
                    HasPrestation = x.HasPrestation,
                    Prestations = x.Prestations,
                    IsAbsent = x.IsAbsent
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
        if (dto == null)
            return BadRequest("Payload invalide.");

        if (dto.Endheure <= dto.Startheure)
            return BadRequest("L'heure de fin doit être postérieure à l'heure de début.");

        if (!int.TryParse(dto.Tolkcode, out var tolkcodeInt))
            return NotFound($"Interprète {dto.Tolkcode} introuvable.");

        var interprete = await _db.Tolkidentities.FindAsync(new object[] { tolkcodeInt }, ct);
        if (interprete == null)
            return NotFound($"Interprète {dto.Tolkcode} introuvable.");

        var idsAff = (dto.IdAffAudiences ?? Array.Empty<int>()).Distinct().ToArray();

        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 1) PAIEMENT : ID explicite depuis la séquence
            var paiementId = await NextValAsync("NR_AUTO_PAIEMENT", ct);

            var paiement = new Paiement
            {
                IdPaiement = paiementId,
                Tolkcode = dto.Tolkcode,
                DatePrestation = dto.DatePrestation.Date,
                Montant = 0,
                Transport = 0,
                Total = 0,
                MontantTva = 0
            };
            _db.Paiements.Add(paiement);
            await _db.SaveChangesAsync(ct);

            // 2) PRESTATION : ID explicite depuis la séquence
            var prestationId = await NextValAsync("ID_PRESTATION_AUTO", ct);

            var prestation = new Prestation
            {
                IdPrestation = prestationId,
                Tolkcode = dto.Tolkcode,
                DatePrestation = dto.DatePrestation.Date,
                Startheure = dto.Startheure,
                Endheure = dto.Endheure,
                UserCreate = User?.Identity?.Name ?? "api",
                IdPaiement = paiement.IdPaiement
            };
            _db.Prestations.Add(prestation);
            await _db.SaveChangesAsync(ct);

            // 3) Lier TOLKLINK -> PRESTATION
            if (idsAff.Length > 0)
            {
                var candidateLinks = await _db.Tolklinks
                    .Where(x => x.Tolkcode == tolkcodeInt && x.Datesupp == null)
                    .ToListAsync(ct);

                var linksToUpdate = candidateLinks
                    .Where(x => x.NrAffAudience.HasValue && idsAff.Contains(x.NrAffAudience.Value))
                    .ToList();

                foreach (var link in linksToUpdate)
                    link.IdPrestation = prestation.IdPrestation;

                if (linksToUpdate.Count > 0)
                    await _db.SaveChangesAsync(ct);
            }

            // 4) Calcul & mise à jour des montants du paiement (selon tes règles)
            await CalculerEtMettreAJourPaiementAsync(prestation, ct);

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
        if (dto == null)
            return BadRequest("Payload invalide.");

        if (!int.TryParse(dto.Tolkcode, out var tolkcodeInt))
            return NotFound($"Interprète {dto.Tolkcode} introuvable.");

        var idsAff = (dto.IdAffAudiences ?? Array.Empty<int>()).Distinct().ToArray();
        if (idsAff.Length == 0)
            return BadRequest("Aucune audience spécifiée.");

        var links = await _db.Tolklinks
            .Where(x => x.Tolkcode == tolkcodeInt
                      && x.NrAffAudience.HasValue
                      && idsAff.Contains(x.NrAffAudience.Value)
                      && x.Datesupp == null)
            .ToListAsync(ct);

        if (links.Count == 0)
            return NotFound("Lien interprète-audience introuvable ou déjà supprimé.");

        foreach (var link in links)
            link.Datesupp = dto.DatePrestation.Date;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST api/prestations/remplacement
    [HttpPost("remplacement")]
    public async Task<IActionResult> Remplacer([FromBody] RemplacementDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            return BadRequest("Payload invalide.");

        if (!int.TryParse(dto.AncienTolkcode, out var ancienInt))
            return NotFound($"Ancien interprète {dto.AncienTolkcode} introuvable.");

        if (!int.TryParse(dto.NouveauTolkcode, out var nouveauInt))
            return NotFound($"Nouvel interprète {dto.NouveauTolkcode} introuvable.");

        var nouveau = await _db.Tolkidentities.FindAsync(new object[] { nouveauInt }, ct);
        if (nouveau == null)
            return NotFound($"Nouvel interprète {dto.NouveauTolkcode} introuvable.");

        var link = await _db.Tolklinks
            .FirstOrDefaultAsync(x => x.Tolkcode == ancienInt
                                   && x.NrAffAudience == dto.IdAffAudience
                                   && x.Datesupp == null, ct);

        if (link == null)
            return NotFound("Lien interprète-audience introuvable ou déjà supprimé.");

        link.Tolkcode = nouveauInt;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Calcule Montant / Transport / TVA / Total d’un paiement rattaché à une prestation.
    /// - Min 75 min
    /// - Transport (2*km plafonné 100) 1x/jour
    /// - TVA si IdStatut == 1
    /// </summary>
    private async Task CalculerEtMettreAJourPaiementAsync(Prestation prestation, CancellationToken ct)
    {
        // 1) Récup Paiement
        var paiement = await _db.Paiements.FirstOrDefaultAsync(p => p.IdPaiement == prestation.IdPaiement, ct);
        if (paiement == null) return;

        // Parse Tolkcode int pour jointures
        if (!int.TryParse(prestation.Tolkcode, out var tolkcodeInt))
            return;

        var date = prestation.DatePrestation.Date;

        // 2) Récup indexation - CHARGEMENT EN MÉMOIRE
        var allIndexations = await _db.Indexations.ToListAsync(ct);

        var idx = allIndexations
            .Where(i => prestation.DatePrestation >= i.Startdate &&
                        (!i.Enddate.HasValue || prestation.DatePrestation < i.Enddate.Value))
            .FirstOrDefault();

        if (idx == null)
            throw new InvalidOperationException("Aucune ligne d'indexation active pour cette date.");

        var euro75 = idx.Euro75min;
        var euroHeure = idx.Euroheure;
        var euroKm = idx.Eurokm;

        // 3) Adresse active (km) à la date
        var adr = await _db.Tolkadresses.AsNoTracking()
            .Where(a => a.Tolkcode == tolkcodeInt.ToString()
                        && a.Startdate <= date
                        && (a.Enddate == null || date < a.Enddate))
            .OrderByDescending(a => a.Startdate)
            .FirstOrDefaultAsync(ct);

        var km = (decimal)(adr?.Km ?? 0);
        var kmAR = Math.Min(100m, 2m * km);

        // 4) Statut TVA à la date
        var tvaStatut = await _db.TolkTvas.AsNoTracking()
            .Where(t => t.Tolkcode == tolkcodeInt
                        && t.StartDate <= date
                        && (t.EndDate == null || date < t.EndDate))
            .OrderByDescending(t => t.StartDate)
            .FirstOrDefaultAsync(ct);

        var assujetti = (tvaStatut?.IdStatut ?? 0) == 1;

        // 5) Durée de cette prestation (en minutes)
        var rawMinutes = (decimal)(prestation.Endheure - prestation.Startheure).TotalMinutes;
        if (rawMinutes < 0) rawMinutes = 0;

        // Arrondi au quart d'heure supérieur
        var minutes = Math.Ceiling(rawMinutes / 15m) * 15m;

        decimal montant;
        if (minutes <= 75m)
        {
            montant = euro75;
        }
        else
        {
            var surplus = minutes - 75m;
            montant = euro75 + surplus * (euroHeure / 60m);
        }

        // 6) Transport : payé une seule fois par jour - CHARGEMENT EN MÉMOIRE
        var prestationsJour = await _db.Prestations
            .Where(pr => pr.Tolkcode == prestation.Tolkcode
                         && pr.DatePrestation == date
                         && pr.IdPrestation != prestation.IdPrestation)
            .Select(pr => pr.IdPaiement)
            .ToListAsync(ct);

        var dejaTransportJour = false;
        if (prestationsJour.Any())
        {
            var paiementsJour = await _db.Paiements
                .Where(pa => prestationsJour.Contains(pa.IdPaiement))
                .ToListAsync(ct);

            dejaTransportJour = paiementsJour.Any(pa => pa.Transport > 0);
        }

        var transport = dejaTransportJour ? 0m : euroKm * kmAR;

        // 7) TVA & Total
        var baseHT = montant + transport;
        var tva = assujetti ? Math.Round(baseHT * TVA_RATE, 2) : 0m;
        var total = baseHT + tva;

        // 8) Mise à jour Paiement
        paiement.Montant = Math.Round(montant, 2);
        paiement.Transport = Math.Round(transport, 2);
        paiement.MontantTva = Math.Round(tva, 2);
        paiement.Total = Math.Round(total, 2);

        await _db.SaveChangesAsync(ct);
    }
}
