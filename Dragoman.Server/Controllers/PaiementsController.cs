using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Dragoman.Server.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Linq;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api/paiements")]
public class PaiementsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PaiementsController(ApplicationDbContext db) => _db = db;

    // GET /api/paiements/mois?month=2025-12
    [HttpGet("mois")]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<PaiementMoisInterpreteRowDto>>> ListInterpretesByMonth(
        [FromQuery] string month,
        CancellationToken ct = default)
    {
        if (!TryParseMonth(month, out var d0, out var d1))
            return BadRequest("Paramètre 'month' attendu au format YYYY-MM (ex: 2025-12).");

        var rows = await (
            from pa in _db.Paiements.AsNoTracking()
            join pr in _db.Prestations.AsNoTracking() on pa.IdPaiement equals pr.IdPaiement
            where pa.DatePrestation >= d0 && pa.DatePrestation < d1
            select new
            {
                pa.Tolkcode,
                Montant = pa.Montant ?? 0m,
                Transport = pa.Transport ?? 0m,
                MontantTva = pa.MontantTva ?? 0m,
                Total = pa.Total ?? 0m
            }
        ).ToListAsync(ct);

        if (rows.Count == 0)
            return Ok(Array.Empty<PaiementMoisInterpreteRowDto>());

        // Identités
        var tolkStr = rows.Select(x => x.Tolkcode).Distinct().ToList();
        var tolkInt = tolkStr
            .Select(s => int.TryParse(s, out var i) ? i : 0)
            .Where(i => i > 0)
            .Distinct()
            .ToList();

        var identities = tolkInt.Count == 0
            ? new Dictionary<string, Tolkidentity>()
            : await _db.Tolkidentities.AsNoTracking()
                .Where(t => tolkInt.Contains(t.Tolkcode))
                .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        var result = rows
            .GroupBy(x => x.Tolkcode)
            .Select(g =>
            {
                identities.TryGetValue(g.Key, out var id);

                return new PaiementMoisInterpreteRowDto
                {
                    Tolkcode = g.Key,
                    Nom = id?.Nom ?? "",
                    Prenom = id?.Prenom ?? "",
                    Taalrol = id?.Taalrol,
                    NbPrestations = g.Count(),
                    Montant = Math.Round(g.Sum(x => x.Montant), 2),
                    Transport = Math.Round(g.Sum(x => x.Transport), 2),
                    MontantTva = Math.Round(g.Sum(x => x.MontantTva), 2),
                    Total = Math.Round(g.Sum(x => x.Total), 2),
                };
            })
            .OrderBy(x => x.Nom)
            .ThenBy(x => x.Prenom)
            .ToList();

        return Ok(result);
    }

    // ✅ IMPORTANT : évite ORA-01722 + évite que "pdf" soit capturé ici
    // GET /api/paiements/mois/{tolkcode}?month=2025-12
    [HttpGet("mois/{tolkcode:int}")]
    [Produces("application/json")]
    public async Task<ActionResult<PaiementMoisDetailDto>> GetDetail(
        int tolkcode,
        [FromQuery] string month,
        CancellationToken ct = default)
    {
        if (!TryParseMonth(month, out var d0, out var d1))
            return BadRequest("Paramètre 'month' attendu au format YYYY-MM (ex: 2025-12).");

        var sCode = tolkcode.ToString();

        var raw = await (
            from pa in _db.Paiements.AsNoTracking()
            join pr in _db.Prestations.AsNoTracking() on pa.IdPaiement equals pr.IdPaiement
            where pa.Tolkcode == sCode
               && pa.DatePrestation >= d0 && pa.DatePrestation < d1
            select new
            {
                pa.IdPaiement,
                pa.Tolkcode,
                Date = pa.DatePrestation,
                pr.Startheure,
                pr.Endheure,
                Montant = pa.Montant ?? 0m,
                Transport = pa.Transport ?? 0m,
                MontantTva = pa.MontantTva ?? 0m,
                Total = pa.Total ?? 0m,
                pa.IdFacture
            }
        ).ToListAsync(ct);

        if (raw.Count == 0)
            return NotFound($"Aucun paiement pour l’interprète {sCode} sur {month}.");

        var id = await _db.Tolkidentities.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tolkcode == tolkcode, ct);

        var adrs = await _db.Tolkadresses.AsNoTracking()
            .Where(a => a.Tolkcode == sCode)
            .ToListAsync(ct);

        decimal KmForDate(DateTime date)
        {
            var d = date.Date;
            var adr = adrs
                .Where(a => a.Startdate.Date <= d && (a.Enddate == null || d < a.Enddate.Value.Date))
                .OrderByDescending(a => a.Startdate)
                .FirstOrDefault();

            return (decimal)(adr?.Km ?? 0);
        }

        var rows = raw
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Startheure)
            .Select(x =>
            {
                var duree = (int)Math.Max(0, (x.Endheure - x.Startheure).TotalMinutes);

                return new PaiementMoisDetailRowDto
                {
                    IdPaiement = x.IdPaiement,
                    Date = x.Date.Date,
                    Debut = x.Startheure.ToString("HH:mm"),
                    Fin = x.Endheure.ToString("HH:mm"),
                    Duree = duree,
                    Km = KmForDate(x.Date),
                    Montant = Math.Round(x.Montant, 2),
                    Transport = Math.Round(x.Transport, 2),
                    IdFacture = x.IdFacture,
                };
            })
            .ToList();

        var totMontant = raw.Sum(x => x.Montant);
        var totTransport = raw.Sum(x => x.Transport);
        var totTva = raw.Sum(x => x.MontantTva);
        var totTotal = raw.Sum(x => x.Total);

        return Ok(new PaiementMoisDetailDto
        {
            Tolkcode = sCode,
            Nom = id?.Nom ?? "",
            Prenom = id?.Prenom ?? "",
            Taalrol = id?.Taalrol,
            Rows = rows,
            Totaux = new PaiementMoisTotauxDto
            {
                Montant = Math.Round(totMontant, 2),
                Transport = Math.Round(totTransport, 2),
                BaseHt = Math.Round(totMontant + totTransport, 2),
                MontantTva = Math.Round(totTva, 2),
                Total = Math.Round(totTotal, 2),
            }
        });
    }

    // ✅ 1 PDF qui contient toutes les factures du mois
    // GET /api/paiements/mois/pdf?month=2025-12
    [HttpGet("mois/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadMonthPdf(
        [FromQuery] string month,
        CancellationToken ct = default)
    {
        if (!TryParseMonth(month, out var d0, out var d1))
            return BadRequest("Paramètre 'month' attendu au format YYYY-MM (ex: 2025-12).");

        var raw = await (
            from pa in _db.Paiements.AsNoTracking()
            join pr in _db.Prestations.AsNoTracking() on pa.IdPaiement equals pr.IdPaiement
            where pa.DatePrestation >= d0 && pa.DatePrestation < d1
            select new
            {
                pa.Tolkcode,
                Date = pa.DatePrestation,
                pr.Startheure,
                pr.Endheure,
                Montant = pa.Montant ?? 0m,
                Transport = pa.Transport ?? 0m,
                MontantTva = pa.MontantTva ?? 0m,
                Total = pa.Total ?? 0m
            }
        ).ToListAsync(ct);

        if (raw.Count == 0)
            return NotFound("Aucun paiement sur ce mois.");

        var tolkStr = raw.Select(x => x.Tolkcode).Distinct().ToList();
        var tolkInt = tolkStr
            .Select(s => int.TryParse(s, out var i) ? i : 0)
            .Where(i => i > 0)
            .Distinct()
            .ToList();

        var identities = tolkInt.Count == 0
            ? new Dictionary<string, Tolkidentity>()
            : await _db.Tolkidentities.AsNoTracking()
                .Where(t => tolkInt.Contains(t.Tolkcode))
                .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        var allAdr = await _db.Tolkadresses.AsNoTracking()
            .Where(a => tolkStr.Contains(a.Tolkcode))
            .ToListAsync(ct);

        var adrByTolk = allAdr.GroupBy(a => a.Tolkcode).ToDictionary(g => g.Key, g => g.ToList());

        static Tolkadresse? PickAdr(List<Tolkadresse>? list, DateTime date)
        {
            if (list == null || list.Count == 0) return null;
            var d = date.Date;

            return list
                .Where(a => a.Startdate.Date <= d && (a.Enddate == null || d < a.Enddate.Value.Date))
                .OrderByDescending(a => a.Startdate)
                .FirstOrDefault()
                ?? list.OrderByDescending(a => a.Startdate).FirstOrDefault();
        }

        static string CustomerBlock(bool isNl) =>
            isNl
                ? "FOD Binnenlandse Zaken\nRaad voor Vreemdelingenbetwistingen\nGaucheretstraat 92-94\n1030 BRUSSEL"
                : "SPF Intérieur\nConseil du Contentieux des Etrangers\nRue Gaucheret 92-94\n1030 BRUXELLES";

        var factures = new List<FactureModel>();

        foreach (var g in raw.GroupBy(x => x.Tolkcode).OrderBy(x => x.Key))
        {
            identities.TryGetValue(g.Key, out var id);

            // Langue: NL si TAALROL=1, sinon FR
            var isNl = (id?.Taalrol == 1);

            adrByTolk.TryGetValue(g.Key, out var adrList);

            // Header: adresse active au début du mois
            var headerAdr = PickAdr(adrList, d0);

            var street = headerAdr == null
                ? ""
                : $"{headerAdr.Rue ?? ""} {headerAdr.Numero ?? ""} {(string.IsNullOrWhiteSpace(headerAdr.Boite) ? "" : "bte " + headerAdr.Boite)}".Trim();

            var city = headerAdr == null
                ? ""
                : $"{headerAdr.Cp ?? ""} {headerAdr.Commune ?? ""}".Trim();

            var vat = !string.IsNullOrWhiteSpace(id?.Tva) ? id!.Tva : null;

            // Compte bancaire: BANKREKENING (BBAN belge 12 chiffres) formaté xxx-xxxxxxx-xx
            var bank = FormatBelgianBban(id?.Bankrekening);

            // Référence: Tolkcode (exigence métier)
            var kenmerk = g.Key;

            // Fedcom: FEDCOMNUMMER (exigence métier)
            var fedcom = id?.Fedcomnummer?.ToString();

            var rows = g
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Startheure)
                .Select(x =>
                {
                    var rowAdr = PickAdr(adrList, x.Date);
                    var km = (decimal)(rowAdr?.Km ?? 0);
                    var duree = (int)Math.Max(0, (x.Endheure - x.Startheure).TotalMinutes);

                    return new FactureRow(
                        x.Date.Date,
                        x.Startheure.ToString("HH:mm"),
                        x.Endheure.ToString("HH:mm"),
                        duree,
                        km,
                        Math.Round(x.Montant, 2),
                        Math.Round(x.Transport, 2)
                    );
                })
                .ToList();

            var totalPresta = g.Sum(x => x.Montant);
            var totalDepl = g.Sum(x => x.Transport);
            var totalTva = g.Sum(x => x.MontantTva);
            var totalTtc = g.Sum(x => x.Total);

            factures.Add(new FactureModel(
                Month: month,
                IsNl: isNl,
                SupplierName: $"{id?.Nom ?? ""} {id?.Prenom ?? ""}".Trim(),
                SupplierStreetLine: street,
                SupplierCityLine: city,
                VatNumber: vat,
                Kenmerk: kenmerk,
                Bank: bank,
                Fedcom: fedcom,
                CustomerBlock: CustomerBlock(isNl),
                Rows: rows,
                TotalPrestation: Math.Round(totalPresta, 2),
                TotalDeplacement: Math.Round(totalDepl, 2),
                TotalBaseHt: Math.Round(totalPresta + totalDepl, 2),
                TotalTva: Math.Round(totalTva, 2),
                TotalTtc: Math.Round(totalTtc, 2)
            ));
        }

        var doc = new FacturesBatchPdfDocument(factures);
        var pdfBytes = doc.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Factures_{month}.pdf");
    }

    // DELETE /api/paiements/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var paiement = await _db.Paiements.FindAsync(new object[] { id }, ct);
        if (paiement == null)
            return NotFound($"Paiement {id} introuvable.");

        if (paiement.IdFacture != null)
            return BadRequest("Impossible de supprimer un paiement déjà facturé.");

        // 1. Libérer IdPrestation dans Tolklink
        var prestations = await _db.Prestations
            .Where(p => p.IdPaiement == id)
            .ToListAsync(ct);

        var prestationIds = prestations.Select(p => p.IdPrestation).ToList();

        if (prestationIds.Count > 0)
        {
            var tolklinks = await _db.Tolklinks
                .Where(tl => tl.IdPrestation != null && prestationIds.Contains(tl.IdPrestation.Value))
                .ToListAsync(ct);

            foreach (var tl in tolklinks)
                tl.IdPrestation = null;
        }

        // 2. Supprimer les Prestations
        _db.Prestations.RemoveRange(prestations);

        // 3. Supprimer le Paiement
        _db.Paiements.Remove(paiement);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? FormatBelgianBban(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // garder uniquement les chiffres
        var digits = new string(input.Where(char.IsDigit).ToArray());

        // BBAN belge = 12 chiffres => xxx-xxxxxxx-xx
        if (digits.Length == 12)
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 7)}-{digits.Substring(10, 2)}";

        // fallback
        return input.Trim();
    }

    private static bool TryParseMonth(string month, out DateTime d0, out DateTime d1)
    {
        d0 = default; d1 = default;
        if (string.IsNullOrWhiteSpace(month)) return false;

        var parts = month.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], out var year)) return false;
        if (!int.TryParse(parts[1], out var mo)) return false;
        if (mo < 1 || mo > 12) return false;

        d0 = new DateTime(year, mo, 1);
        d1 = d0.AddMonths(1);
        return true;
    }
}
