using System.Data;
using System.Text;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Dragoman.Server.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using QuestPDF.Fluent;

namespace Dragoman.Server.Controllers;

[ApiController]
[Route("api/factures")]
public class FacturesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public FacturesController(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    // =============================================
    // GET /api/factures?month=&statut=&tolkcode=
    // =============================================
    [HttpGet]
    public async Task<ActionResult<List<FactureListItemDto>>> List(
        [FromQuery] string? month,
        [FromQuery] string? statut,
        [FromQuery] string? tolkcode,
        CancellationToken ct)
    {
        IQueryable<Facture> query = _db.Factures.AsNoTracking();

        // Filtrer par mois de prestation (et non par date de génération)
        if (!string.IsNullOrWhiteSpace(month) && TryParseMonth(month, out var d0, out var d1))
        {
            var factureIdsForMonth = _db.Paiements.AsNoTracking()
                .Where(p => p.IdFacture != null
                            && p.DatePrestation >= d0
                            && p.DatePrestation < d1)
                .Select(p => p.IdFacture!.Value)
                .Distinct();

            // Inclure les factures annulées et notes de crédit par DateGeneration
            // (leurs paiements ont été supprimés, impossible de les trouver via Paiement.DatePrestation)
            query = query.Where(f =>
                factureIdsForMonth.Contains(f.IdFacture)
                || ((f.StatutFacture == "NOTE DE CREDIT"
                     || f.StatutFacture == "CREDIT VALIDE"
                     || f.StatutFacture == "ANNULEE"
                     || f.StatutFacture == "TRANSMISE")
                    && f.DateGeneration >= d0
                    && f.DateGeneration < d1));
        }

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(f => f.StatutFacture == statut);

        if (!string.IsNullOrWhiteSpace(tolkcode))
            query = query.Where(f => f.Tolkcode == tolkcode);

        var factures = await query.OrderBy(f => f.IdFacture).ToListAsync(ct);

        if (factures.Count == 0)
            return Ok(new List<FactureListItemDto>());

        var tolkStrs = factures.Select(f => f.Tolkcode).Distinct().ToList();
        var tolkInts = tolkStrs.Select(s => int.TryParse(s, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();

        var identities = tolkInts.Count == 0
            ? new Dictionary<string, Tolkidentity>()
            : await _db.Tolkidentities.AsNoTracking()
                .Where(t => tolkInts.Contains(t.Tolkcode))
                .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        var factureIds = factures.Select(f => f.IdFacture).ToList();
        var countByFacture = await _db.Paiements.AsNoTracking()
            .Where(p => p.IdFacture != null && factureIds.Contains(p.IdFacture!.Value))
            .GroupBy(p => p.IdFacture!.Value)
            .Select(g => new { Id = g.Key, Nb = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Nb, ct);

        var result = factures.Select(f =>
        {
            identities.TryGetValue(f.Tolkcode, out var id);
            countByFacture.TryGetValue(f.IdFacture, out var nb);
            return new FactureListItemDto
            {
                IdFacture = f.IdFacture,
                Reference = $"RVV-CCE/{f.IdFacture}",
                Tolkcode = f.Tolkcode,
                Nom = id?.Nom ?? "",
                Prenom = id?.Prenom ?? "",
                DateGeneration = f.DateGeneration,
                DateValidationFedcom = f.DateValidationFedcom,
                StatutFacture = f.StatutFacture,
                TotalTtc = f.TotalTtc,
                NbPaiements = nb,
                DateTransmission = f.DateTransmission,
            };
        }).ToList();

        return Ok(result);
    }

    // =============================================
    // POST /api/factures/generer
    // =============================================
    [HttpPost("generer")]
    public async Task<IActionResult> Generer([FromBody] GenererFacturesRequestDto req, CancellationToken ct)
    {
        DateTime d0, d1;

        // Mode période (dateDebut / dateFin)
        if (!string.IsNullOrWhiteSpace(req.DateDebut) && !string.IsNullOrWhiteSpace(req.DateFin))
        {
            if (!DateTime.TryParse(req.DateDebut, out d0))
                return BadRequest("DateDebut invalide. Format attendu : YYYY-MM-DD");
            if (!DateTime.TryParse(req.DateFin, out d1))
                return BadRequest("DateFin invalide. Format attendu : YYYY-MM-DD");
            d1 = d1.Date.AddDays(1); // inclure le jour de fin
            if (d0 >= d1)
                return BadRequest("DateDebut doit être antérieure à DateFin.");
        }
        // Mode mois (annee / mois)
        else
        {
            if (req.Mois < 1 || req.Mois > 12) return BadRequest("Mois invalide.");
            if (req.Annee < 2000 || req.Annee > 2100) return BadRequest("Année invalide.");
            d0 = new DateTime(req.Annee, req.Mois, 1);
            d1 = d0.AddMonths(1);
        }

        var paiements = await _db.Paiements
            .Where(p => p.IdFacture == null && p.DatePrestation >= d0 && p.DatePrestation < d1)
            .ToListAsync(ct);

        if (paiements.Count == 0)
            return Ok(new { created = 0, linked = 0 });

        int created = 0, linked = 0;
        using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var g in paiements.GroupBy(p => p.Tolkcode))
        {
            if (string.IsNullOrWhiteSpace(g.Key) || !int.TryParse(g.Key, out _))
                continue;

            var facture = new Facture
            {
                Tolkcode = g.Key,
                DateGeneration = DateTime.Now,
                StatutFacture = "GENEREE",
                TotalTtc = g.Sum(x => x.Total ?? 0m)
            };

            _db.Factures.Add(facture);
            await _db.SaveChangesAsync(ct);

            foreach (var p in g)
                p.IdFacture = facture.IdFacture;

            await _db.SaveChangesAsync(ct);
            created++;
            linked += g.Count();
        }

        await tx.CommitAsync(ct);
        return Ok(new { created, linked });
    }

    // =============================================
    // PATCH /api/factures/{id}/statut
    // =============================================
    [HttpPatch("{id:int}/statut")]
    public async Task<IActionResult> UpdateStatut(int id, [FromBody] UpdateStatutDto dto, CancellationToken ct)
    {
        var allowed = new[] { "APPROUVEE", "ANNULEE" };
        var statut = dto.StatutFacture?.Trim().ToUpperInvariant() ?? "";

        if (!allowed.Contains(statut))
            return BadRequest($"Statut invalide. Valeurs acceptées : {string.Join(", ", allowed)}");

        var facture = await _db.Factures.FindAsync(new object[] { id }, ct);
        if (facture == null)
            return NotFound($"Facture {id} introuvable.");

        // Empêcher d'annuler une note de crédit (elle est déjà une annulation)
        if (statut == "ANNULEE" && (facture.StatutFacture == "NOTE DE CREDIT" || facture.StatutFacture == "CREDIT VALIDE"))
            return BadRequest("Impossible d'annuler une note de crédit.");

        // On ne peut annuler que si la facture a été validée par Fedcom
        if (statut == "ANNULEE" && facture.StatutFacture != "APPROUVEE")
            return BadRequest("Impossible d'annuler une facture qui n'a pas été validée par Fedcom.");

        // On ne peut approuver que si la facture a été transmise (ou est une note de crédit)
        if (statut == "APPROUVEE" && facture.StatutFacture != "TRANSMISE" && facture.StatutFacture != "NOTE DE CREDIT")
            return BadRequest("Impossible de valider Fedcom tant que la facture n'a pas été transmise.");

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (statut == "ANNULEE")
        {
            // 1. Récupérer les paiements liés à cette facture
            var paiements = await _db.Paiements
                .Where(p => p.IdFacture == id)
                .ToListAsync(ct);

            var paiementIds = paiements.Select(p => p.IdPaiement).ToList();

            // 2. Récupérer les prestations liées à ces paiements
            var prestations = await _db.Prestations
                .Where(pr => paiementIds.Contains(pr.IdPaiement))
                .ToListAsync(ct);

            var prestationIds = prestations.Select(pr => pr.IdPrestation).ToList();

            // 3. Mettre à null IdPrestation dans Tolklink pour libérer les audiences
            var tolklinks = await _db.Tolklinks
                .Where(tl => tl.IdPrestation != null && prestationIds.Contains(tl.IdPrestation!.Value))
                .ToListAsync(ct);

            foreach (var tl in tolklinks)
                tl.IdPrestation = null;

            await _db.SaveChangesAsync(ct);

            // 4. Créer la note de crédit avec lien vers la facture originale
            var noteDeCredit = new Facture
            {
                Tolkcode = facture.Tolkcode,
                DateGeneration = DateTime.Now,
                StatutFacture = "NOTE DE CREDIT",
                TotalTtc = -facture.TotalTtc,
                IdFactureOrigine = facture.IdFacture
            };

            _db.Factures.Add(noteDeCredit);
            await _db.SaveChangesAsync(ct);

            // 5. Copier les paiements vers la note de crédit (montants négatifs) via SQL brut
            //    Le provider Oracle EF Core ne gère pas correctement NEXTVAL lors d'Add/SaveChanges
            //    quand d'autres entités avec la même PK sont (ou étaient) dans le change tracker.
            var newPaiementIdMap = new Dictionary<int, int>();
            foreach (var p in paiements)
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct);

                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    @"INSERT INTO PAIEMENT (ID_PAIEMENT, TOLKCODE, DATE_PRESTATION, MONTANT, TRANSPORT, TOTAL, MONTANT_TVA, ID_FACTURE)
                      VALUES (NR_AUTO_PAIEMENT.NEXTVAL, :tk, :dp, :mt, :tr, :tot, :tva, :idf)
                      RETURNING ID_PAIEMENT INTO :newid";

                // Use the current transaction
                cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

                void AddParam(string name, object? val, DbType dbType)
                {
                    var prm = cmd.CreateParameter();
                    prm.ParameterName = name;
                    prm.DbType = dbType;
                    prm.Value = val ?? DBNull.Value;
                    cmd.Parameters.Add(prm);
                }

                AddParam("tk", p.Tolkcode, DbType.String);
                AddParam("dp", p.DatePrestation, DbType.Date);
                AddParam("mt", (object?)(-(p.Montant ?? 0m)), DbType.Decimal);
                AddParam("tr", (object?)(-(p.Transport ?? 0m)), DbType.Decimal);
                AddParam("tot", (object?)(-(p.Total ?? 0m)), DbType.Decimal);
                AddParam("tva", (object?)(-(p.MontantTva ?? 0m)), DbType.Decimal);
                AddParam("idf", noteDeCredit.IdFacture, DbType.Int32);

                // Output parameter for RETURNING INTO
                var outParam = cmd.CreateParameter();
                outParam.ParameterName = "newid";
                outParam.DbType = DbType.Int32;
                outParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outParam);

                await cmd.ExecuteNonQueryAsync(ct);
                var newId = Convert.ToInt32(outParam.Value);
                newPaiementIdMap[p.IdPaiement] = newId;
            }

            // 6. Copier les prestations liées (heures identiques, pour le PDF) via SQL brut
            foreach (var pr in prestations)
            {
                if (!newPaiementIdMap.TryGetValue(pr.IdPaiement, out var newPaiementId))
                    continue;

                var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct);

                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    @"INSERT INTO PRESTATION (ID_PRESTATION, TOLKCODE, DATE_PRESTATION, STARTHEURE, ENDHEURE, USER_CREATE, ID_PAIEMENT)
                      VALUES (ID_PRESTATION_AUTO.NEXTVAL, :tk, :dp, :sh, :eh, :uc, :idp)";

                cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

                void AddParam2(string name, object? val, DbType dbType)
                {
                    var prm = cmd.CreateParameter();
                    prm.ParameterName = name;
                    prm.DbType = dbType;
                    prm.Value = val ?? DBNull.Value;
                    cmd.Parameters.Add(prm);
                }

                AddParam2("tk", pr.Tolkcode, DbType.String);
                AddParam2("dp", pr.DatePrestation, DbType.Date);
                AddParam2("sh", pr.Startheure, DbType.DateTime);
                AddParam2("eh", pr.Endheure, DbType.DateTime);
                AddParam2("uc", (object?)pr.UserCreate ?? DBNull.Value, DbType.String);
                AddParam2("idp", newPaiementId, DbType.Int32);

                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 7. Détacher les entités originales du tracking EF avant suppression par SQL brut
            foreach (var pr in prestations)
                _db.Entry(pr).State = EntityState.Detached;
            foreach (var p in paiements)
                _db.Entry(p).State = EntityState.Detached;

            // 8. Supprimer les prestations originales (FK Tolklink déjà libérée)
            if (prestationIds.Count > 0)
            {
                var prestaIdList = string.Join(",", prestationIds);
                await _db.Database.ExecuteSqlRawAsync(
                    $"DELETE FROM PRESTATION WHERE ID_PRESTATION IN ({prestaIdList})", ct);
            }

            // 9. Supprimer les paiements originaux (FK Prestation libérée)
            if (paiementIds.Count > 0)
            {
                var paiIdList = string.Join(",", paiementIds);
                await _db.Database.ExecuteSqlRawAsync(
                    $"DELETE FROM PAIEMENT WHERE ID_PAIEMENT IN ({paiIdList})", ct);
            }
        }

        // 7. Marquer la facture originale (ou approuver une note de crédit)
        if (statut == "APPROUVEE" && facture.StatutFacture == "NOTE DE CREDIT")
        {
            facture.StatutFacture = "CREDIT VALIDE";
            facture.DateValidationFedcom = DateTime.Now;
        }
        else if (statut == "APPROUVEE" && facture.StatutFacture == "TRANSMISE")
        {
            // Facture transmise puis validée par Fedcom
            facture.StatutFacture = "APPROUVEE";
            facture.DateValidationFedcom = DateTime.Now;
        }
        else if (statut == "APPROUVEE")
        {
            facture.StatutFacture = "APPROUVEE";
            facture.DateValidationFedcom = DateTime.Now;
        }
        else if (statut == "ANNULEE")
        {
            facture.StatutFacture = "ANNULEE";
            // Conserver DateValidationFedcom pour l'historique
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new
        {
            facture.IdFacture,
            Reference = $"RVV-CCE/{facture.IdFacture}",
            facture.StatutFacture,
            facture.DateValidationFedcom
        });
    }

    // =============================================
    // PATCH /api/factures/{id}/transmettre
    // =============================================
    [HttpPatch("{id:int}/transmettre")]
    public async Task<IActionResult> Transmettre(int id, CancellationToken ct)
    {
        var facture = await _db.Factures.FindAsync(new object[] { id }, ct);
        if (facture == null)
            return NotFound($"Facture {id} introuvable.");

        facture.DateTransmission = DateTime.Now;
        facture.StatutFacture = "TRANSMISE";

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            facture.IdFacture,
            Reference = $"RVV-CCE/{facture.IdFacture}",
            facture.StatutFacture,
            facture.DateTransmission
        });
    }

    // =============================================
    // GET /api/factures/pdf?month=2025-06&po=4501133577
    // =============================================
    [HttpGet("pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadPdf(
        [FromQuery] string month,
        [FromQuery] string? po,
        CancellationToken ct)
    {
        if (!TryParseMonth(month, out var d0, out var d1))
            return BadRequest("Format attendu : YYYY-MM");

        // Factures ayant des paiements sur le mois (par date de prestation)
        var factureIdsForMonth = await _db.Paiements.AsNoTracking()
            .Where(p => p.IdFacture != null
                        && p.DatePrestation >= d0
                        && p.DatePrestation < d1)
            .Select(p => p.IdFacture!.Value)
            .Distinct()
            .ToListAsync(ct);

        var facturesDb = await _db.Factures.AsNoTracking()
            .Where(f => factureIdsForMonth.Contains(f.IdFacture)
                        && f.StatutFacture != "ANNULEE")
            .OrderBy(f => f.IdFacture)
            .ToListAsync(ct);

        if (facturesDb.Count == 0)
            return NotFound("Aucune facture sur ce mois.");

        // Paiements liés
        var factureIds = facturesDb.Select(f => f.IdFacture).ToList();
        var paiements = await _db.Paiements.AsNoTracking()
            .Where(p => p.IdFacture != null && factureIds.Contains(p.IdFacture!.Value))
            .ToListAsync(ct);

        // Prestations (heures début/fin)
        var paiementIds = paiements.Select(p => p.IdPaiement).ToList();
        var prestations = await _db.Prestations.AsNoTracking()
            .Where(pr => paiementIds.Contains(pr.IdPaiement))
            .ToDictionaryAsync(pr => pr.IdPaiement, ct);

        // Identités
        var tolkStrs = facturesDb.Select(f => f.Tolkcode).Distinct().ToList();
        var tolkInts = tolkStrs.Select(s => int.TryParse(s, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
        var identities = tolkInts.Count == 0
            ? new Dictionary<string, Tolkidentity>()
            : await _db.Tolkidentities.AsNoTracking()
                .Where(t => tolkInts.Contains(t.Tolkcode))
                .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

        // Adresses
        var allAdr = await _db.Tolkadresses.AsNoTracking()
            .Where(a => tolkStrs.Contains(a.Tolkcode))
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

        static string CustomerBlock(bool isNl) => isNl
            ? "Account Payable\nLeuvenseweg 1\n1000 BRUSSEL\naccountspayable@ibz.be"
            : "Account Payable\n1 Rue de Louvain\n1000 BRUXELLES\naccountspayable@ibz.be";

        // TVA statut actif par tolkcode (le plus récent sans EndDate, ou le plus récent dont la période couvre d0)
        var tolkIntsForTva = facturesDb.Select(f => int.TryParse(f.Tolkcode, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
        var allTva = await _db.TolkTvas.AsNoTracking()
            .Where(t => tolkIntsForTva.Contains(t.Tolkcode))
            .ToListAsync(ct);
        var tvaByTolk = allTva.GroupBy(t => t.Tolkcode).ToDictionary(g => g.Key, g => g.ToList());

        static byte? PickTvaStatut(List<TolkTva>? list, DateTime refDate)
        {
            if (list == null || list.Count == 0) return null;
            var d = refDate.Date;
            var active = list
                .Where(t => t.StartDate.HasValue && t.StartDate.Value.Date <= d && (t.EndDate == null || d <= t.EndDate.Value.Date))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();
            return active?.IdStatut ?? list.Where(t => t.EndDate == null).OrderByDescending(t => t.StartDate).FirstOrDefault()?.IdStatut;
        }

        var pdfModels = new List<FactureModel>();

        foreach (var fDb in facturesDb)
        {
            identities.TryGetValue(fDb.Tolkcode, out var id);
            var isNl = id?.Taalrol == 1;

            adrByTolk.TryGetValue(fDb.Tolkcode, out var adrList);
            var headerAdr = PickAdr(adrList, d0);

            var street = headerAdr == null ? ""
                : $"{headerAdr.Rue ?? ""} {headerAdr.Numero ?? ""} {(string.IsNullOrWhiteSpace(headerAdr.Boite) ? "" : "bte " + headerAdr.Boite)}".Trim();
            var city = headerAdr == null ? ""
                : $"{headerAdr.Cp ?? ""} {headerAdr.Commune ?? ""}".Trim();

            var vat = !string.IsNullOrWhiteSpace(id?.Tva) ? id!.Tva : null;
            var bank = FormatBelgianBban(id?.Bankrekening);
            var fedcom = id?.Fedcomnummer?.ToString();

            var facPaiements = paiements
                .Where(p => p.IdFacture == fDb.IdFacture)
                .OrderBy(p => p.DatePrestation)
                .ToList();

            var rows = facPaiements.Select(p =>
            {
                prestations.TryGetValue(p.IdPaiement, out var pr);
                var rowAdr = PickAdr(adrList, p.DatePrestation);
                var km = (decimal)(rowAdr?.Km ?? 0);
                var duree = pr != null
                    ? (int)Math.Max(0, (pr.Endheure - pr.Startheure).TotalMinutes)
                    : 0;

                return new FactureRow(
                    p.DatePrestation.Date,
                    pr?.Startheure.ToString("HH:mm") ?? "",
                    pr?.Endheure.ToString("HH:mm") ?? "",
                    duree, km,
                    Math.Round(p.Montant ?? 0m, 2),
                    Math.Round(p.Transport ?? 0m, 2)
                );
            }).ToList();

            var totalPresta = facPaiements.Sum(p => p.Montant ?? 0m);
            var totalDepl = facPaiements.Sum(p => p.Transport ?? 0m);
            var totalTva = facPaiements.Sum(p => p.MontantTva ?? 0m);
            var totalTtc = facPaiements.Sum(p => p.Total ?? 0m);

            var tolkInt2 = int.TryParse(fDb.Tolkcode, out var ti2) ? ti2 : 0;
            tvaByTolk.TryGetValue(tolkInt2, out var tvaList);
            var tvaStatut = PickTvaStatut(tvaList, d0);

            pdfModels.Add(new FactureModel(
                Month: month,
                IsNl: isNl,
                SupplierName: $"{id?.Nom ?? ""} {id?.Prenom ?? ""}".Trim(),
                SupplierStreetLine: street,
                SupplierCityLine: city,
                VatNumber: vat,
                Kenmerk: fDb.Tolkcode,
                Bank: bank,
                Fedcom: fedcom,
                CustomerBlock: CustomerBlock(isNl),
                Rows: rows,
                TotalPrestation: Math.Round(totalPresta, 2),
                TotalDeplacement: Math.Round(totalDepl, 2),
                TotalBaseHt: Math.Round(totalPresta + totalDepl, 2),
                TotalTva: Math.Round(totalTva, 2),
                TotalTtc: Math.Round(totalTtc, 2),
                Reference: $"RVV-CCE/{fDb.IdFacture}",
                PoNumber: po?.Trim(),
                Ondernemingsnummer: "0308356862",
                TvaStatutId: tvaStatut
            ));
        }

        var doc = new FacturesBatchPdfDocument(pdfModels);
        var pdfBytes = doc.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Factures_{month}.pdf");
    }

    // =============================================
    // GET /api/factures/{id}/eml?po=4501133577
    // Génère un fichier .eml qui ouvre Outlook avec
    // le PDF en pièce jointe, sujet et corps pré-remplis
    // =============================================
    [HttpGet("{id:int}/eml")]
    public async Task<IActionResult> GenerateEml(int id, [FromQuery] string? po, CancellationToken ct)
    {
        var facture = await _db.Factures.AsNoTracking().FirstOrDefaultAsync(f => f.IdFacture == id, ct);
        if (facture == null)
            return NotFound($"Facture {id} introuvable.");

        if (!int.TryParse(facture.Tolkcode, out var tolkInt))
            return BadRequest("Tolkcode invalide.");

        var identity = await _db.Tolkidentities.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tolkcode == tolkInt, ct);

        if (identity == null)
            return NotFound($"Interprète {facture.Tolkcode} introuvable.");

        var recipientEmail = identity.Email?.Trim();
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return BadRequest($"L'interprète {identity.Nom} {identity.Prenom} (#{facture.Tolkcode}) n'a pas d'adresse email.");

        // Build the PDF for this single facture
        var paiements = await _db.Paiements.AsNoTracking()
            .Where(p => p.IdFacture == id)
            .ToListAsync(ct);

        var paiementIds = paiements.Select(p => p.IdPaiement).ToList();
        var prestations = await _db.Prestations.AsNoTracking()
            .Where(pr => paiementIds.Contains(pr.IdPaiement))
            .ToDictionaryAsync(pr => pr.IdPaiement, ct);

        var allAdr = await _db.Tolkadresses.AsNoTracking()
            .Where(a => a.Tolkcode == facture.Tolkcode)
            .ToListAsync(ct);

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

        static string CustomerBlock(bool isNl) => isNl
            ? "Account Payable\nLeuvenseweg 1\n1000 BRUSSEL\naccountspayable@ibz.be"
            : "Account Payable\n1 Rue de Louvain\n1000 BRUXELLES\naccountspayable@ibz.be";

        var isNoteDeCredit = facture.StatutFacture == "NOTE DE CREDIT" || facture.StatutFacture == "CREDIT VALIDE";

        // For a note de crédit, find the original facture reference via IdFactureOrigine
        string? originalReference = null;
        if (isNoteDeCredit)
        {
            if (facture.IdFactureOrigine.HasValue)
            {
                originalReference = $"RVV-CCE/{facture.IdFactureOrigine.Value}";
            }
            else
            {
                // Fallback for old credit notes without IdFactureOrigine
                var originalFacture = await _db.Factures.AsNoTracking()
                    .Where(f => f.Tolkcode == facture.Tolkcode
                              && f.StatutFacture == "ANNULEE"
                              && f.IdFacture < facture.IdFacture)
                    .OrderByDescending(f => f.IdFacture)
                    .FirstOrDefaultAsync(ct);
                if (originalFacture != null)
                    originalReference = $"RVV-CCE/{originalFacture.IdFacture}";
            }
        }

        var isNl = identity.Taalrol == 1;
        var refDate = paiements.Count > 0 ? paiements.Min(p => p.DatePrestation) : facture.DateGeneration;
        var headerAdr = PickAdr(allAdr, refDate);

        var street = headerAdr == null ? ""
            : $"{headerAdr.Rue ?? ""} {headerAdr.Numero ?? ""} {(string.IsNullOrWhiteSpace(headerAdr.Boite) ? "" : "bte " + headerAdr.Boite)}".Trim();
        var city = headerAdr == null ? ""
            : $"{headerAdr.Cp ?? ""} {headerAdr.Commune ?? ""}".Trim();

        var vat = !string.IsNullOrWhiteSpace(identity.Tva) ? identity.Tva : null;
        var bank = FormatBelgianBban(identity.Bankrekening);
        var fedcom = identity.Fedcomnummer?.ToString();

        var month = refDate.ToString("yyyy-MM");

        // TVA statut actif pour cet interprète
        var tolkTvaList = await _db.TolkTvas.AsNoTracking()
            .Where(t => t.Tolkcode == tolkInt)
            .ToListAsync(ct);
        byte? emlTvaStatut = null;
        {
            var d = refDate.Date;
            var active = tolkTvaList
                .Where(t => t.StartDate.HasValue && t.StartDate.Value.Date <= d && (t.EndDate == null || d <= t.EndDate.Value.Date))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();
            emlTvaStatut = active?.IdStatut ?? tolkTvaList.Where(t => t.EndDate == null).OrderByDescending(t => t.StartDate).FirstOrDefault()?.IdStatut;
        }

        var rows = paiements.OrderBy(p => p.DatePrestation).Select(p =>
        {
            prestations.TryGetValue(p.IdPaiement, out var pr);
            var rowAdr = PickAdr(allAdr, p.DatePrestation);
            var km = (decimal)(rowAdr?.Km ?? 0);
            var duree = pr != null
                ? (int)Math.Max(0, (pr.Endheure - pr.Startheure).TotalMinutes)
                : 0;
            return new FactureRow(
                p.DatePrestation.Date,
                pr?.Startheure.ToString("HH:mm") ?? "",
                pr?.Endheure.ToString("HH:mm") ?? "",
                duree, km,
                Math.Round(p.Montant ?? 0m, 2),
                Math.Round(p.Transport ?? 0m, 2)
            );
        }).ToList();

        var totalPresta = paiements.Sum(p => p.Montant ?? 0m);
        var totalDepl = paiements.Sum(p => p.Transport ?? 0m);
        var totalTva = paiements.Sum(p => p.MontantTva ?? 0m);
        var totalTtc = paiements.Sum(p => p.Total ?? 0m);

        var pdfModel = new FactureModel(
            Month: month,
            IsNl: isNl,
            SupplierName: $"{identity.Nom ?? ""} {identity.Prenom ?? ""}".Trim(),
            SupplierStreetLine: street,
            SupplierCityLine: city,
            VatNumber: vat,
            Kenmerk: facture.Tolkcode,
            Bank: bank,
            Fedcom: fedcom,
            CustomerBlock: CustomerBlock(isNl),
            Rows: rows,
            TotalPrestation: Math.Round(totalPresta, 2),
            TotalDeplacement: Math.Round(totalDepl, 2),
            TotalBaseHt: Math.Round(totalPresta + totalDepl, 2),
            TotalTva: Math.Round(totalTva, 2),
            TotalTtc: Math.Round(totalTtc, 2),
            Reference: $"RVV-CCE/{facture.IdFacture}",
            PoNumber: po?.Trim(),
            Ondernemingsnummer: "0308356862",
            IsNoteDeCredit: isNoteDeCredit,
            OriginalReference: originalReference,
            TvaStatutId: emlTvaStatut
        );

        var doc = new FacturesBatchPdfDocument(new[] { pdfModel });
        var pdfBytes = doc.GeneratePdf();

        // Build .eml (RFC 2822 MIME message) that Outlook will open as a draft
        var reference = $"RVV-CCE/{facture.IdFacture}";
        string subject, bodyText, pdfFileName;

        // Peppol e-invoicing notice (NL / FR / EN)
        const string peppolNotice =
            "\r\n\r\n---\r\n\r\n"
            + "1. Vanaf januari 2026 is e-facturatie via Peppol in principe verplicht. "
            + "Voor meer informatie over het nieuw facturatieproces en het gebruik van Peppol verwijzen we naar het document in bijlage.\r\n"
            + "2. In een overgangsperiode kan u uw facturen tijdelijk nog indienen per mail:\r\n"
            + "   - Voor prestaties vanaf februari 2026 gebruikt u hiervoor het mailadres accountspayable@ibz.be.\r\n"
            + "   - Voor prestaties tot en met januari 2026 gebruikt u nog het adres tolken.Interprete.rvv-cce@ibz.be. "
            + "Gelieve de nog niet ingediende prestaties zonder uitstel in te dienen.\r\n\r\n"
            + "De RVV raadt u evenwel aan om zo snel mogelijk over te schakelen op Peppol.\r\n\r\n"
            + "---\r\n\r\n"
            + "1. À partir de janvier 2026, la facturation électronique via Peppol sera en principe obligatoire. "
            + "Pour plus d'informations sur le nouveau processus de facturation et l'utilisation de Peppol, veuillez consulter le document joint.\r\n"
            + "2. Pendant une période de transition, vous pouvez encore soumettre temporairement vos factures par e-mail :\r\n"
            + "   - Pour les prestations à partir de février 2026, vous utilisez l'adresse e-mail accountspayable@ibz.be.\r\n"
            + "   - Pour les prestations jusqu'à janvier 2026 compris, vous utilisez encore l'adresse tolken.Interprete.rvv-cce@ibz.be. "
            + "Veuillez soumettre sans délai les factures qui ne sont pas encore transmises.\r\n\r\n"
            + "Le CCE vous conseille d'opter pour Peppol dès que possible.\r\n\r\n"
            + "---\r\n\r\n"
            + "1. From January 2026, e-invoicing via Peppol will in principle be mandatory. "
            + "For more information about the new invoicing process and the use of Peppol, please refer to the attached document.\r\n"
            + "2. During a transitional period, you can temporarily still submit your invoices by e-mail:\r\n"
            + "   - For services from February 2026 onwards, use the e-mail address accountspayable@ibz.be.\r\n"
            + "   - For services up to and including January 2026, you still use the address tolken.Interprete.rvv-cce@ibz.be. "
            + "Please submit the services not yet submitted without delay.\r\n\r\n"
            + "The Council advises you to switch to Peppol as soon as possible.";

        if (isNoteDeCredit)
        {
            subject = isNl
                ? $"Creditnota {reference} van prestatieoverzicht {originalReference ?? ""} — {month}"
                : $"Note de crédit {reference} de l'aperçu des prestations {originalReference ?? ""} — {month}";
            bodyText = isNl
                ? $"Beste ,\r\n\r\nIn bijlage vindt u de creditnota {reference} betreffende prestatieoverzicht {originalReference ?? ""} voor de periode {month}.\r\n\r\nMet vriendelijke groeten,\r\nCCE — Raad voor Vreemdelingenbetwistingen"
                : $"Bonjour ,\r\n\r\nVeuillez trouver en pièce jointe la note de crédit {reference} relative à l'aperçu des prestations {originalReference ?? ""} pour la période {month}.\r\n\r\nCordialement,\r\nCCE — Conseil du Contentieux des Étrangers";
            bodyText += peppolNotice;
            pdfFileName = $"Creditnota_{reference.Replace("/", "-")}_{month}.pdf";
        }
        else
        {
            subject = isNl
                ? $"Uw prestatieoverzicht {reference} — {month}"
                : $"Votre aperçu des prestations {reference} — {month}";
            bodyText = isNl
                ? $"Beste,\r\n\r\nIn bijlage vindt u het prestatieoverzicht {reference} voor de periode {month}.\r\n\r\nMet vriendelijke groeten,\r\nCCE — Raad voor Vreemdelingenbetwistingen"
                : $"Bonjour ,\r\n\r\nVeuillez trouver en pièce jointe l'aperçu des prestations {reference} pour la période {month}.\r\n\r\nCordialement,\r\nCCE — Conseil du Contentieux des Étrangers";
            bodyText += peppolNotice;
            pdfFileName = $"Prestatieoverzicht_{reference.Replace("/", "-")}_{month}.pdf";
        }
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        var boundary = $"----=_Part_{Guid.NewGuid():N}";

        // Try to read the Peppol document from the configured path
        byte[]? peppolDocBytes = null;
        string? peppolDocName = null;
        var peppolPath = _config["PeppolDocumentPath"];
        if (!string.IsNullOrWhiteSpace(peppolPath))
        {
            var fullPath = Path.IsPathRooted(peppolPath)
                ? peppolPath
                : Path.Combine(_env.ContentRootPath, peppolPath);
            if (System.IO.File.Exists(fullPath))
            {
                peppolDocBytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
                peppolDocName = Path.GetFileName(fullPath);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"To: {recipientEmail}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("X-Unsent: 1");
        sb.AppendLine($"MIME-Version: 1.0");
        sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        sb.AppendLine();
        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/plain; charset=utf-8");
        sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
        sb.AppendLine();
        sb.AppendLine(bodyText);
        sb.AppendLine();
        sb.AppendLine($"--{boundary}");
        sb.AppendLine($"Content-Type: application/pdf; name=\"{pdfFileName}\"");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine($"Content-Disposition: attachment; filename=\"{pdfFileName}\"");
        sb.AppendLine();

        // Write PDF base64 in 76-char lines
        for (var i = 0; i < pdfBase64.Length; i += 76)
            sb.AppendLine(pdfBase64.Substring(i, Math.Min(76, pdfBase64.Length - i)));

        // Attach the Peppol document if available
        if (peppolDocBytes != null && peppolDocName != null)
        {
            var docBase64 = Convert.ToBase64String(peppolDocBytes);
            sb.AppendLine();
            sb.AppendLine($"--{boundary}");
            sb.AppendLine($"Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document; name=\"{peppolDocName}\"");
            sb.AppendLine("Content-Transfer-Encoding: base64");
            sb.AppendLine($"Content-Disposition: attachment; filename=\"{peppolDocName}\"");
            sb.AppendLine();

            for (var i = 0; i < docBase64.Length; i += 76)
                sb.AppendLine(docBase64.Substring(i, Math.Min(76, docBase64.Length - i)));
        }

        sb.AppendLine($"--{boundary}--");

        var emlBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var emlFileName = isNoteDeCredit
            ? $"Creditnota_{reference.Replace("/", "-")}_{month}.eml"
            : $"Prestatieoverzicht_{reference.Replace("/", "-")}_{month}.eml";

        return File(emlBytes, "message/rfc822", emlFileName);
    }

    // =============================================
    private static string? FormatBelgianBban(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 12)
            return $"{digits[..3]}-{digits[3..10]}-{digits[10..12]}";
        return input.Trim();
    }

    private static bool TryParseMonth(string? month, out DateTime d0, out DateTime d1)
    {
        d0 = default; d1 = default;
        if (string.IsNullOrWhiteSpace(month)) return false;
        var parts = month.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var mo)) return false;
        if (mo < 1 || mo > 12) return false;
        d0 = new DateTime(year, mo, 1);
        d1 = d0.AddMonths(1);
        return true;
    }
}

public class UpdateStatutDto
{
    public string StatutFacture { get; set; } = "";
}