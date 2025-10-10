using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;

namespace Dragoman.Server.Controllers
{
    public record HDTicketDto(string? date, string? heure, string? numero, string? type, int? dureeMin);
    public record HDAutreTacheDto(string? denomination, string? date, int? dureeMin);

    public record HDPrestationJourDto(
        string hdUser,
        string hdDate,
        string hdSemaineISO,
        string? hdRegimeTravail,
        string? hdGarde,
        List<HDTicketDto> hdTickets,
        List<HDAutreTacheDto> hdAutresTaches,
        string? hdRemarquesCollaborateur
    );

    public record HDPrestationSemaineDto(string hdUser, string hdSemaineISO, List<HDPrestationJourDto> jours);

    [ApiController]
    [Route("api/hd-prestations")]
    public class HdPrestationsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private string BasePath => Path.Combine(AppContext.BaseDirectory, "data", "hd-prestations");

        // ====== Enregistrer jour
        [HttpPost("jour")]
        public async Task<IActionResult> EnregistrerJour([FromBody] HDPrestationJourDto dto, CancellationToken ct)
        {
            if (!ValiderJour(dto, out var msg)) return BadRequest(new { ok = false, message = msg });
            var dir = Path.Combine(BasePath, San(dto.hdUser), San(dto.hdSemaineISO));
            Directory.CreateDirectory(dir);
            var fileName = $"{San(dto.hdUser)}_{San(dto.hdSemaineISO)}_{San(dto.hdDate)}.json";
            var path = Path.Combine(dir, fileName);
            await using var fs = System.IO.File.Create(path);
            await JsonSerializer.SerializeAsync(fs, dto, JsonOpts, ct);
            return Ok(new { ok = true, chemin = path.Replace(AppContext.BaseDirectory, ".\\") });
        }

        // ====== Lire jour
        [HttpGet("jour")]
        public IActionResult LireJour([FromQuery] string hdUser, [FromQuery] string hdDate)
        {
            var candidates = UserDirCandidates(hdUser);
            // calc semaine ISO de la date
            var dt = DateTime.Parse(hdDate, CultureInfo.InvariantCulture);
            var week = ISOWeek.GetWeekOfYear(dt);
            var semaineISO = $"{dt.Year}-W{week:00}";

            foreach (var userDir in candidates)
            {
                var dir = Path.Combine(BasePath, userDir, San(semaineISO));
                var path = Path.Combine(dir, $"{San(hdUser)}_{San(semaineISO)}_{San(hdDate)}.json");
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                    return Content(json, "application/json", Encoding.UTF8);
                }
            }
            // fallback: chercher *_{date}.json partout
            foreach (var userDir in candidates)
            {
                var udir = Path.Combine(BasePath, userDir);
                if (!Directory.Exists(udir)) continue;
                var files = Directory.GetFiles(udir, $"*_{San(hdDate)}.json", SearchOption.AllDirectories);
                var f = files.FirstOrDefault();
                if (f != null)
                {
                    var json = System.IO.File.ReadAllText(f, Encoding.UTF8);
                    return Content(json, "application/json", Encoding.UTF8);
                }
            }
            return Ok(null);
        }

        // ====== Lire semaine
        [HttpGet("semaine")]
        public IActionResult LireSemaine([FromQuery] string hdUser, [FromQuery] string hdSemaineISO)
        {
            var jours = ChargerSemaineSmart(hdUser, hdSemaineISO);
            return Ok(jours);
        }

        // ====== Enregistrer semaine
        [HttpPut("semaine")]
        public async Task<IActionResult> EnregistrerSemaine([FromBody] HDPrestationSemaineDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.hdUser)) return BadRequest(new { ok = false, message = "hdUser manquant" });
            if (string.IsNullOrWhiteSpace(dto.hdSemaineISO)) return BadRequest(new { ok = false, message = "hdSemaineISO manquant" });

            var dir = Path.Combine(BasePath, San(dto.hdUser), San(dto.hdSemaineISO));
            Directory.CreateDirectory(dir);

            foreach (var j in dto.jours ?? new())
            {
                if (!ValiderJour(j, out var msg)) return BadRequest(new { ok = false, message = $"Jour invalide ({j.hdDate}): {msg}" });
                var path = Path.Combine(dir, $"{San(j.hdUser)}_{San(j.hdSemaineISO)}_{San(j.hdDate)}.json");
                await using var fs = System.IO.File.Create(path);
                await JsonSerializer.SerializeAsync(fs, j, JsonOpts, ct);
            }
            return Ok(new { ok = true, chemin = dir.Replace(AppContext.BaseDirectory, ".\\") });
        }

        // ====== Export Word
        [HttpGet("semaine/export/word")]
        public IActionResult ExporterSemaineWord([FromQuery] string hdUser, [FromQuery] string hdSemaineISO)
        {
            var jours = ChargerSemaineSmart(hdUser, hdSemaineISO);
            if (jours.Count == 0) return NotFound("Aucune donnée.");
            using var ms = new MemoryStream();
            GenererDocxFiche(jours, ms, hdUser, hdSemaineISO);
            ms.Position = 0;
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"FicheHelpdesk_{San(hdUser)}_{San(hdSemaineISO)}.docx");
        }

        // ====== Helpers
        private static bool ValiderJour(HDPrestationJourDto dto, out string? message)
        {
            if (string.IsNullOrWhiteSpace(dto.hdUser)) { message = "hdUser manquant"; return false; }
            if (string.IsNullOrWhiteSpace(dto.hdDate)) { message = "hdDate manquant"; return false; }
            if (string.IsNullOrWhiteSpace(dto.hdSemaineISO)) { message = "hdSemaineISO manquant"; return false; }
            message = null; return true;
        }

        private static string San(string s) =>
            string.Join("_", s.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

        private List<string> UserDirCandidates(string hdUser)
        {
            var list = new List<string>();
            var orig = San(hdUser); list.Add(orig);
            var parts = hdUser.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var last = San(parts.Last());
                if (!list.Contains(last)) list.Add(last);
            }
            return list;
        }

        private static (DateOnly monday, List<string> dates) WeekDatesFromISO(string hdSemaineISO)
        {
            var year = int.Parse(hdSemaineISO.Substring(0, 4));
            var week = int.Parse(hdSemaineISO.Substring(6, 2));
            var mondayDt = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
            var monday = DateOnly.FromDateTime(mondayDt);
            var list = Enumerable.Range(0, 7).Select(i => monday.AddDays(i).ToString("yyyy-MM-dd")).ToList();
            return (monday, list);
        }

        private List<HDPrestationJourDto> ChargerSemaineSmart(string hdUser, string hdSemaineISO)
        {
            var result = new List<HDPrestationJourDto>();
            var candidates = UserDirCandidates(hdUser);

            // 1) direct
            foreach (var userDir in candidates)
            {
                var dir = Path.Combine(BasePath, userDir, San(hdSemaineISO));
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
                    {
                        var txt = System.IO.File.ReadAllText(f, Encoding.UTF8);
                        var obj = JsonSerializer.Deserialize<HDPrestationJourDto>(txt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (obj != null) result.Add(obj);
                    }
                    if (result.Count > 0) return result.OrderBy(x => x.hdDate).ToList();
                }
            }

            // 2) fallback par dates
            var (_, dates) = WeekDatesFromISO(hdSemaineISO);
            foreach (var userDir in candidates)
            {
                var udir = Path.Combine(BasePath, userDir);
                if (!Directory.Exists(udir)) continue;

                foreach (var d in dates)
                {
                    var files = Directory.GetFiles(udir, $"*_{San(d)}.json", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        var txt = System.IO.File.ReadAllText(f, Encoding.UTF8);
                        var obj = JsonSerializer.Deserialize<HDPrestationJourDto>(txt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (obj != null) result.Add(obj);
                    }
                }
            }

            return result.GroupBy(x => x.hdDate).Select(g => g.First()).OrderBy(x => x.hdDate).ToList();
        }

        // ===== OpenXML utilitaires
        private static TableCell Cell(string txt, bool header = false, string width = "2400")
        {
            var props = new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = width });
            if (header)
                props.Append(new Shading() { Color = "auto", Fill = "E8EEF9", Val = ShadingPatternValues.Clear });
            return new TableCell(new Paragraph(new Run(new Text(txt ?? "")))) { TableCellProperties = props };
        }
        private static Table CreateTableWithBorders()
        {
            var t = new Table();
            var pr = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                ));
            t.AppendChild(pr);
            return t;
        }

        private static void GenererDocxFiche(List<HDPrestationJourDto> jours, Stream output, string user, string semaineIso)
        {
            using var doc = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document, true);
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;
            var culture = CultureInfo.GetCultureInfo("fr-BE");

            string Safe(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!;

            // Résumé semaine
            var regimes = jours.Select(j => j.hdRegimeTravail?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
            string regimeSemaine = "—";
            if (regimes.Count > 0)
            {
                var grp = regimes.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).ToList();
                regimeSemaine = grp.Count == 1 ? grp[0].Key : $"mixte ({string.Join(", ", grp.Select(g => g.Key))})";
            }
            var gardes = jours.Select(j => j.hdGarde?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
            var gardeOui = gardes.Any(s => s.StartsWith("oui", StringComparison.OrdinalIgnoreCase));
            var gardeDet = gardes.Select(s => s.Contains('—') ? s.Split('—', 2)[1].Trim() : "")
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                 .Distinct(StringComparer.OrdinalIgnoreCase);
            string gardeSemaine = gardeOui ? ("oui" + (gardeDet.Any() ? $" — {string.Join(", ", gardeDet)}" : "")) : "non";

            int nbTickets = jours.Sum(j => j.hdTickets?.Count ?? 0);
            int nbAutres = jours.Sum(j => j.hdAutresTaches?.Count ?? 0);
            int totTicketsMin = jours.Sum(j => (j.hdTickets ?? new()).Sum(t => Math.Max(0, t.dureeMin ?? 0)));
            int totAutresMin = jours.Sum(j => (j.hdAutresTaches ?? new()).Sum(a => Math.Max(0, a.dureeMin ?? 0)));
            int totGeneral = totTicketsMin + totAutresMin;

            body.Append(
                new Paragraph(new Run(new Text("FICHE DE PRESTATIONS HELPDESK — RÉCAP SEMAINE")))
                {
                    ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "160" })
                },
                new Paragraph(new Run(new Text($"Utilisateur : {user}"))) { ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "60" }) },
                new Paragraph(new Run(new Text($"Semaine : {semaineIso}"))) { ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "160" }) }
            );

            var resume = CreateTableWithBorders();
            resume.Append(new TableRow(Cell("Régime de travail (semaine)", true, "4000"), Cell(Safe(regimeSemaine), false, "6000")));
            resume.Append(new TableRow(Cell("Garde (semaine)", true, "4000"), Cell(Safe(gardeSemaine), false, "6000")));
            resume.Append(new TableRow(Cell("Tickets — nombre", true, "4000"), Cell(nbTickets.ToString(), false, "6000")));
            resume.Append(new TableRow(Cell("Tickets — minutes", true, "4000"), Cell(totTicketsMin.ToString(), false, "6000")));
            resume.Append(new TableRow(Cell("Autres tâches — nombre", true, "4000"), Cell(nbAutres.ToString(), false, "6000")));
            resume.Append(new TableRow(Cell("Autres tâches — minutes", true, "4000"), Cell(totAutresMin.ToString(), false, "6000")));
            resume.Append(new TableRow(Cell("TOTAL général (minutes)", true, "4000"), Cell(totGeneral.ToString(), false, "6000")));
            body.Append(resume);
            body.Append(new Paragraph(new Run(new Text(" "))));

            // Détails par jour
            foreach (var j in jours.OrderBy(x => x.hdDate))
            {
                bool aContenu = (j.hdTickets?.Any() ?? false) || (j.hdAutresTaches?.Any() ?? false) || !string.IsNullOrWhiteSpace(j.hdRemarquesCollaborateur);
                if (!aContenu) continue;

                var d = DateOnly.Parse(j.hdDate);
                body.Append(new Paragraph(new Run(new Text(d.ToString("dddd dd/MM/yyyy", culture))))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { Before = "180", After = "60" })
                });

                if (j.hdTickets?.Any() ?? false)
                {
                    var t = CreateTableWithBorders();
                    t.Append(new TableRow(Cell("Heure", true, "1800"), Cell("N°", true, "1800"), Cell("Type", true, "3800"), Cell("Durée (min)", true, "1600")));
                    int jourTotT = 0;
                    foreach (var it in j.hdTickets!)
                    {
                        var h = Safe(it.heure); var n = Safe(it.numero); var y = Safe(it.type);
                        var m = Math.Max(0, it.dureeMin ?? 0); jourTotT += m;
                        t.Append(new TableRow(Cell(h, false, "1800"), Cell(n, false, "1800"), Cell(y, false, "3800"), Cell(m.ToString(), false, "1600")));
                    }
                    t.Append(new TableRow(Cell("Total tickets (min)", true, "7400"), Cell(jourTotT.ToString(), true, "1600")));
                    body.Append(t);
                }

                if (j.hdAutresTaches?.Any() ?? false)
                {
                    var a = CreateTableWithBorders();
                    a.Append(new TableRow(Cell("Dénomination", true, "5800"), Cell("Durée (min)", true, "2000")));
                    int jourTotA = 0;
                    foreach (var it in j.hdAutresTaches!)
                    {
                        var lib = Safe(it.denomination); var m = Math.Max(0, it.dureeMin ?? 0); jourTotA += m;
                        a.Append(new TableRow(Cell(lib, false, "5800"), Cell(m.ToString(), false, "2000")));
                    }
                    a.Append(new TableRow(Cell("Total autres tâches (min)", true, "5800"), Cell(jourTotA.ToString(), true, "2000")));
                    body.Append(a);
                }

                if (!string.IsNullOrWhiteSpace(j.hdRemarquesCollaborateur))
                {
                    body.Append(
                        new Paragraph(new Run(new Text("Remarques :"))) { ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { Before = "60", After = "20" }) },
                        new Paragraph(new Run(new Text(j.hdRemarquesCollaborateur!))) { ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "120" }) }
                    );
                }
            }

            // Signature
            body.Append(new Paragraph(new Run(new Text(" "))), new Paragraph(new Run(new Text(" "))));
            var sign = CreateTableWithBorders();
            sign.Append(new TableRow(Cell("Validé par le responsable", true, "5000"), Cell("Signature du collaborateur", true, "5000")));
            sign.Append(new TableRow(
                Cell("Nom et fonction : ____________________________\nDate : ____/____/______\nSignature :", false, "5000"),
                Cell("Nom : ____________________________\nDate : ____/____/______\nSignature :", false, "5000")
            ));
            body.Append(sign);

            main.Document.Save();
        }
    }
}
