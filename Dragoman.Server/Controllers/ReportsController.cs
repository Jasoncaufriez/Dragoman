// Controllers/ReportsController.cs
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) { _db = db; }

    // 1) JSON (pour l’écran/aperçu)
    [HttpGet("interpretes")]
    public async Task<IActionResult> GetInterpretes([FromQuery] DateTime? date, CancellationToken ct)
    {
        var jour = (date ?? DateTime.Today).Date;

        var rows = await _db.Set<ReportInterpreteRow>()
            .AsNoTracking()
            .Where(r => r.Jour == jour)
            .OrderBy(r => r.HeureAudience)
            .ThenBy(r => r.SalleAudience)
            .ThenBy(r => r.Nom).ThenBy(r => r.Prenom)
            .ToListAsync(ct);

        var map = new Dictionary<int?, InterpretePresenceDto>();
        foreach (var r in rows)
        {
            if (!map.TryGetValue(r.Tolkcode, out var item))
            {
                item = new InterpretePresenceDto
                {
                    Tolkcode = r.Tolkcode,
                    Nom = r.Nom,
                    Prenom = r.Prenom,
                    Telephone = string.Join(" / ", new[] { r.Gsm, r.Tel, r.Telbis }.Where(s => !string.IsNullOrWhiteSpace(s)))
                };
                map[r.Tolkcode] = item;
            }
            item.Audiences.Add(new InterpreteAudienceDto
            {
                Heure = r.HeureAudience,
                Salle = r.SalleAudience,
                Langue = r.LangueRequete
            });
        }

        return Ok(map.Values.ToList());
    }

    // 2) Excel
    [HttpGet("interpretes/excel")]
    public async Task<IActionResult> ExportInterpretesExcel([FromQuery] DateTime? date, CancellationToken ct)
    {
        var jour = (date ?? DateTime.Today).Date;
        var data = await GetData(jour, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Présence interprètes");

        // Entête
        ws.Cell(1, 1).Value = $"Présence interprètes — {jour:yyyy-MM-dd}";
        ws.Range(1, 1, 1, 7).Merge().Style
            .Font.SetBold().Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Header tableau
        var r = 3;
        ws.Cell(r, 1).Value = "Présent";
        ws.Cell(r, 2).Value = "Interprète (#)";
        ws.Cell(r, 3).Value = "Téléphone";
        ws.Cell(r, 4).Value = "Heure";
        ws.Cell(r, 5).Value = "Salle";
        ws.Cell(r, 6).Value = "Langue";
        ws.Cell(r, 7).Value = "Remarque";
        ws.Range(r, 1, r, 7).Style
           .Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#eef2ff"))
           .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
           .Border.SetInsideBorder(XLBorderStyleValues.Thin);
        r++;

        // Lignes
        foreach (var i in data)
        {
            if (i.Audiences.Count == 0)
            {
                ws.Cell(r, 1).Value = "";
                ws.Cell(r, 2).Value = $"{i.Nom} {i.Prenom} (#{i.Tolkcode})";
                ws.Cell(r, 3).Value = i.Telephone;
                ws.Range(r, 1, r, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                r++;
                continue;
            }

            foreach (var a in i.Audiences)
            {
                ws.Cell(r, 1).Value = ""; // case présence (à cocher sur papier)
                ws.Cell(r, 2).Value = $"{i.Nom} {i.Prenom} (#{i.Tolkcode})";
                ws.Cell(r, 3).Value = i.Telephone;
                ws.Cell(r, 4).Value = a.Heure;
                ws.Cell(r, 5).Value = a.Salle;
                ws.Cell(r, 6).Value = a.Langue;
                ws.Cell(r, 7).Value = "";
                ws.Range(r, 1, r, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                r++;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        var fname = $"Presence_Interpretes_{jour:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fname);
    }

    // 3) Word (optionnel, simple tableau)
    [HttpGet("interpretes/word")]
    public async Task<IActionResult> ExportInterpretesWord([FromQuery] DateTime? date, CancellationToken ct)
    {
        var jour = (date ?? DateTime.Today).Date;
        var data = await GetData(jour, ct);

        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());

            var body = main.Document.Body!;
            body.Append(new Paragraph(new Run(new Text($"Présence interprètes — {jour:yyyy-MM-dd}")))
            {
                ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center })
            });

            // tableau
            var table = new Table(
                new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 6 },
                        new BottomBorder { Val = BorderValues.Single, Size = 6 },
                        new LeftBorder { Val = BorderValues.Single, Size = 6 },
                        new RightBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                    )
                )
            );

            // header
            table.Append(MkRow("Présent", "Interprète (#)", "Téléphone", "Heure", "Salle", "Langue", "Remarque"));

            foreach (var i in data)
            {
                if (i.Audiences.Count == 0)
                    table.Append(MkRow("", $"{i.Nom} {i.Prenom} (#{i.Tolkcode})", i.Telephone ?? "", "", "", "", ""));
                else
                    foreach (var a in i.Audiences)
                        table.Append(MkRow("", $"{i.Nom} {i.Prenom} (#{i.Tolkcode})", i.Telephone ?? "", a.Heure ?? "", a.Salle ?? "", a.Langue ?? "", ""));
            }

            body.Append(table);
            main.Document.Save();
        }

        var bytes = ms.ToArray();
        var fname = $"Presence_Interpretes_{jour:yyyy-MM-dd}.docx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fname);

        // local function
        static TableRow MkRow(params string[] cells)
        {
            var tr = new TableRow();
            foreach (var c in cells)
                tr.Append(new TableCell(new Paragraph(new Run(new Text(c ?? "")))));
            return tr;
        }
    }

    private async Task<List<InterpretePresenceDto>> GetData(DateTime jour, CancellationToken ct)
    {
        var rows = await _db.Set<ReportInterpreteRow>()
            .AsNoTracking()
            .Where(r => r.Jour == jour)
            .OrderBy(r => r.HeureAudience).ThenBy(r => r.SalleAudience).ThenBy(r => r.Nom).ThenBy(r => r.Prenom)
            .ToListAsync(ct);

        var map = new Dictionary<int?, InterpretePresenceDto>();
        foreach (var r in rows)
        {
            if (!map.TryGetValue(r.Tolkcode, out var item))
            {
                item = new InterpretePresenceDto
                {
                    Tolkcode = r.Tolkcode,
                    Nom = r.Nom,
                    Prenom = r.Prenom,
                    Telephone = string.Join(" / ", new[] { r.Gsm, r.Tel, r.Telbis }.Where(s => !string.IsNullOrWhiteSpace(s)))
                };
                map[r.Tolkcode] = item;
            }
            item.Audiences.Add(new InterpreteAudienceDto { Heure = r.HeureAudience, Salle = r.SalleAudience, Langue = r.LangueRequete });
        }
        return map.Values.OrderBy(x => x.Nom).ThenBy(x => x.Prenom).ToList();
    }
}
