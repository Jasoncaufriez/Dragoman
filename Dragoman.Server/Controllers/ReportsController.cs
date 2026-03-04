using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dragoman.Server.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ReportsController(ApplicationDbContext db) { _db = db; }

        // 1) JSON (aperçu écran)
        [HttpGet("interpretes")]
        public async Task<IActionResult> GetInterpretes([FromQuery] DateOnly date, CancellationToken ct = default)
        {
            var data = await GetData(date, ct);
            return Ok(data);
        }

        // 2) Excel
        [HttpGet("interpretes/excel")]
        public async Task<IActionResult> ExportInterpretesExcel([FromQuery] DateOnly date, CancellationToken ct = default)
        {
            var jour = date;
            var data = await GetData(jour, ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Présence interprètes");

            // Titre
            ws.Cell(1, 1).Value = $"Présence interprètes — {jour:yyyy-MM-dd}";
            ws.Range(1, 1, 1, 9).Merge().Style
                .Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Interprètes avec plusieurs salles (pour astérisque)
            var multiSalle = data
                .Where(i => i.Audiences
                    .Select(a => a.Salle)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .Skip(1).Any())
                .Select(i => i.Tolkcode)
                .Where(c => c.HasValue)
                .Select(c => c!.Value)
                .ToHashSet();

            // Lignes détaillées triées par heure puis salle
            var lignes = data
                .SelectMany(i => i.Audiences.Select(a => new { Interprete = i, Audience = a }))
                .OrderBy(x => x.Audience.Heure)
                .ThenBy(x => x.Audience.Salle)
                .ThenBy(x => x.Interprete.Nom)
                .ThenBy(x => x.Interprete.Prenom)
                .ToList();

            // Interprètes sans audience
            var sansAudience = data
                .Where(i => i.Audiences.Count == 0)
                .OrderBy(i => i.Nom)
                .ThenBy(i => i.Prenom)
                .Select(i => new { Interprete = i, Audience = (InterpreteAudienceDto?)null });

            // En-têtes
            var r = 3;
            ws.Cell(r, 1).Value = "Présent";
            ws.Cell(r, 2).Value = "Heure";
            ws.Cell(r, 3).Value = "Salle";
            ws.Cell(r, 4).Value = "Interprète (#*)";
            ws.Cell(r, 5).Value = "Téléphone";
            ws.Cell(r, 6).Value = "Langue";
            ws.Cell(r, 7).Value = "Aff.";
            ws.Cell(r, 8).Value = "FR/NL";
            ws.Cell(r, 9).Value = "Remarque";

            ws.Range(r, 1, r, 9).Style
               .Font.SetBold()
               .Fill.SetBackgroundColor(XLColor.FromHtml("#eef2ff"))
               .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
               .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            r++;

            void WriteRow(InterpretePresenceDto i, InterpreteAudienceDto? a)
            {
                bool hasMultiSalle = i.Tolkcode.HasValue && multiSalle.Contains(i.Tolkcode.Value);

                ws.Cell(r, 1).Value = "";                 // Présent
                ws.Cell(r, 2).Value = a?.Heure ?? "";     // Heure
                ws.Cell(r, 3).Value = a?.Salle ?? "";     // Salle

                var texteInterprete = $"{i.Nom} {i.Prenom} (#{i.Tolkcode}{(hasMultiSalle ? "*" : "")})";
                ws.Cell(r, 4).Value = texteInterprete;
                ws.Cell(r, 4).Style.Font.SetBold();

                ws.Cell(r, 5).Value = string.Join(" / ", i.Telephones);
                ws.Cell(r, 6).Value = a?.Langue ?? "";
                ws.Cell(r, 7).Value = a?.NbAffaires ?? 0; // Aff.
                ws.Cell(r, 8).Value = i.FrNl ?? "";
                ws.Cell(r, 9).Value = "";                 // Remarque

                ws.Range(r, 1, r, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                r++;
            }

            foreach (var x in lignes)
                WriteRow(x.Interprete, x.Audience);

            foreach (var x in sansAudience)
                WriteRow(x.Interprete, x.Audience);

            ws.Columns().AdjustToContents();
            ws.Column(4).Width = Math.Max(ws.Column(4).Width, 35);
            ws.Column(9).Width = Math.Max(ws.Column(9).Width, 40);

            ws.Cell(r + 1, 1).Value = "* Interprète présent dans plusieurs salles.";

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var fname = $"Presence_Interpretes_{jour:yyyy-MM-dd}.xlsx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fname);
        }

        // 3) Word — 1 page par audience + 1 page synthèse
        [HttpGet("interpretes/word")]
        public async Task<IActionResult> ExportInterpretesWord([FromQuery] DateOnly date, CancellationToken ct = default)
        {
            var jour = date;
            var data = await GetData(jour, ct);

            // Regrouper par (Heure, Salle) = 1 audience
            var audiences = data
                .SelectMany(i => i.Audiences.Select(a => new { Interprete = i, Audience = a }))
                .GroupBy(x => new { x.Audience.Heure, x.Audience.Salle })
                .Select(g => new
                {
                    Heure = g.Key.Heure ?? "",
                    Salle = g.Key.Salle ?? "",
                    Magistrats = string.Join(", ",
                        g.Select(x => x.Audience.Magistrat)
                         .Where(m => !string.IsNullOrWhiteSpace(m))
                         .Distinct()),
                    Interpretes = g
                        .GroupBy(x => x.Interprete.Tolkcode)
                        .Select(ig =>
                        {
                            var interp = ig.First().Interprete;
                            var langues = string.Join(", ",
                                ig.Select(x => x.Audience.Langue)
                                  .Where(l => !string.IsNullOrWhiteSpace(l))
                                  .Distinct());
                            return new { Interprete = interp, Langues = langues };
                        })
                        .OrderBy(x => x.Interprete.Nom)
                        .ThenBy(x => x.Interprete.Prenom)
                        .ToList()
                })
                .OrderBy(a => a.Heure)
                .ThenBy(a => a.Salle)
                .ToList();

            using var ms = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
                var body = main.Document.Body!;

                // === Landscape section properties (used as page break between sections) ===
                SectionProperties MkLandscapeSection() => new SectionProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.PageSize
                    {
                        Width = 16838,
                        Height = 11906,
                        Orient = PageOrientationValues.Landscape
                    },
                    new PageMargin
                    {
                        Top = 720, Right = 720, Bottom = 720, Left = 720,
                        Header = 720, Footer = 720, Gutter = 0
                    }
                );

                // ============================================================
                // PAGE par audience (heure/salle)
                // ============================================================
                for (int idx = 0; idx < audiences.Count; idx++)
                {
                    var aud = audiences[idx];

                    // Titre : "Audience — 10:00 — Salle A12"
                    var titre = $"Audience — {aud.Heure} — Salle {aud.Salle}";
                    body.Append(new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        new Run(new RunProperties(new Bold(), new FontSize { Val = "28" }),
                                new Text(titre)))
                    );

                    // Sous-titre magistrat
                    if (!string.IsNullOrWhiteSpace(aud.Magistrats))
                    {
                        body.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                            new Run(new RunProperties(new Italic()),
                                    new Text($"Magistrat : {aud.Magistrats}")))
                        );
                    }

                    body.Append(new Paragraph()); // espace

                    // Tableau pour cette audience
                    var table = MkTable();
                    var hdr = new TableRow();
                    hdr.Append(MkHeaderCell("Présent", "1000"));
                    hdr.Append(MkHeaderCell("#", "800"));
                    hdr.Append(MkHeaderCell("Interprète", "4000"));
                    hdr.Append(MkHeaderCell("Téléphone", "2600"));
                    hdr.Append(MkHeaderCell("Langue", "2600"));
                    hdr.Append(MkHeaderCell("Aud.", "700"));
                    hdr.Append(MkHeaderCell("FR/NL", "1000"));
                    hdr.Append(MkHeaderCell("Remarque", "4000"));
                    table.Append(hdr);

                    foreach (var x in aud.Interpretes)
                    {
                        var i = x.Interprete;
                        var tr = new TableRow();
                        tr.Append(MkCell(""));
                        tr.Append(MkCell(i.Tolkcode?.ToString() ?? ""));
                        tr.Append(MkBoldCell($"{i.Nom} {i.Prenom}"));
                        tr.Append(MkCell(string.Join(" / ", i.Telephones)));
                        tr.Append(MkCell(x.Langues));
                        tr.Append(MkCell(i.Audiences.Count.ToString()));
                        tr.Append(MkCell(i.FrNl ?? ""));
                        tr.Append(MkCell(""));
                        table.Append(tr);
                    }

                    body.Append(table);

                    body.Append(new Paragraph(
                        new Run(new RunProperties(new FontSize { Val = "18" }),
                                new Text($"{aud.Interpretes.Count} interprète(s) pour cette audience.")))
                    );

                    // Saut de page (section break) sauf après la dernière audience
                    // On insère un SectionProperties dans un paragraphe pour forcer le saut
                    body.Append(new Paragraph(
                        new ParagraphProperties(new SectionProperties(
                            new DocumentFormat.OpenXml.Wordprocessing.PageSize
                            {
                                Width = 16838, Height = 11906,
                                Orient = PageOrientationValues.Landscape
                            },
                            new PageMargin
                            {
                                Top = 720, Right = 720, Bottom = 720, Left = 720,
                                Header = 720, Footer = 720, Gutter = 0
                            }
                        ))
                    ));
                }

                // ============================================================
                // PAGE SYNTHÈSE
                // ============================================================
                body.Append(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new Bold(), new FontSize { Val = "28" }),
                            new Text($"Synthèse — Présence interprètes — {jour:yyyy-MM-dd}")))
                );

                body.Append(new Paragraph()); // espace

                var synthTable = MkTable();
                var synthHdr = new TableRow();
                synthHdr.Append(MkHeaderCell("Présent", "800"));
                synthHdr.Append(MkHeaderCell("#", "700"));
                synthHdr.Append(MkHeaderCell("Interprète", "3500"));
                synthHdr.Append(MkHeaderCell("Téléphone", "2300"));
                synthHdr.Append(MkHeaderCell("Magistrat", "2300"));
                synthHdr.Append(MkHeaderCell("Heure", "900"));
                synthHdr.Append(MkHeaderCell("Salle", "700"));
                synthHdr.Append(MkHeaderCell("Langue", "2000"));
                synthHdr.Append(MkHeaderCell("Aff.", "600"));
                synthHdr.Append(MkHeaderCell("FR/NL", "700"));
                synthHdr.Append(MkHeaderCell("Remarque", "2800"));
                synthTable.Append(synthHdr);

                // Toutes les lignes par interprète/audience, triées heure/salle
                var synthLines = data
                    .SelectMany(i => i.Audiences.Select(a => new { Interprete = i, Audience = a }))
                    .OrderBy(x => x.Audience.Heure)
                    .ThenBy(x => x.Audience.Salle)
                    .ThenBy(x => x.Interprete.Nom)
                    .ThenBy(x => x.Interprete.Prenom)
                    .ToList();

                foreach (var x in synthLines)
                {
                    var i = x.Interprete;
                    var tr = new TableRow();
                    tr.Append(MkCell(""));
                    tr.Append(MkCell(i.Tolkcode?.ToString() ?? ""));
                    tr.Append(MkBoldCell($"{i.Nom} {i.Prenom}"));
                    tr.Append(MkCell(string.Join(" / ", i.Telephones)));
                    tr.Append(MkCell(x.Audience.Magistrat ?? ""));
                    tr.Append(MkCell(x.Audience.Heure ?? ""));
                    tr.Append(MkCell(x.Audience.Salle ?? ""));
                    tr.Append(MkCell(x.Audience.Langue ?? ""));
                    tr.Append(MkCell(x.Audience.NbAffaires.ToString()));
                    tr.Append(MkCell(i.FrNl ?? ""));
                    tr.Append(MkCell(""));
                    synthTable.Append(tr);
                }

                body.Append(synthTable);

                body.Append(new Paragraph(
                    new Run(new RunProperties(new FontSize { Val = "18" }),
                            new Text($"Total : {data.Count} interprète(s) — {data.Sum(d => d.NbAffaires)} affaire(s).")))
                );

                // Section finale (landscape)
                body.Append(MkLandscapeSection());
                main.Document.Save();
            }

            var bytes = ms.ToArray();
            var fname = $"Presence_Interpretes_{jour:yyyy-MM-dd}.docx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fname);

            // === Helpers locaux ===
            static Table MkTable() => new Table(
                new TableProperties(
                    new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
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

            static TableCell MkHeaderCell(string text, string width) =>
                new TableCell(
                    new TableCellProperties(
                        new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width },
                        new Shading { Val = ShadingPatternValues.Clear, Fill = "EEF2FF" }
                    ),
                    new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        new Run(new RunProperties(new Bold()), new Text(text))
                    )
                );

            static TableCell MkCell(string text) =>
                new TableCell(
                    new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }))
                );

            static TableCell MkBoldCell(string text) =>
                new TableCell(
                    new Paragraph(new Run(new RunProperties(new Bold()), new Text(text)))
                );
        }

        // 4) Données regroupées (JSON/Excel/Word/PDF) avec fallback si la vue est vide
        private async Task<List<InterpretePresenceDto>> GetData(DateOnly jour, CancellationToken ct)
        {
            var d0 = jour.ToDateTime(TimeOnly.MinValue);
            var d1 = d0.AddDays(1);

            // a) Source principale : vue
            var rows = await _db.VAudienceInterpreteDetail
                .AsNoTracking()
                .Where(r => r.Jour.HasValue && r.Jour.Value >= d0 && r.Jour.Value < d1)
                .OrderBy(r => r.HeureAudience)
                .ThenBy(r => r.SalleAudience)
                .ThenBy(r => r.Nom)
                .ThenBy(r => r.Prenom)
                .ToListAsync(ct);

            // Récupérer les noms des magistrats depuis VUE_CALENDAR_ALL pour cette date
            var tolkcodesFromView = rows
                .Where(r => r.Tolkcode.HasValue)
                .Select(r => (decimal)r.Tolkcode!.Value)
                .Distinct()
                .ToList();

            var calendarRows = tolkcodesFromView.Count > 0
                ? await _db.VueCalendarVrmPcs.AsNoTracking()
                    .Where(v => v.DateAudience.HasValue
                             && v.DateAudience.Value >= d0
                             && v.DateAudience.Value < d1
                             && v.Tolkcode.HasValue
                             && tolkcodesFromView.Contains(v.Tolkcode.Value))
                    .Select(v => new
                    {
                        Tolkcode = (int)v.Tolkcode!.Value,
                        v.HeureAudience,
                        v.SalleAudience,
                        Magistrat = v.Nom
                    })
                    .ToListAsync(ct)
                : new();

            // Dictionnaire (tolkcode, heure, salle) -> magistrat
            var magistratMap = calendarRows
                .GroupBy(c => (c.Tolkcode, c.HeureAudience, c.SalleAudience))
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.Magistrat).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct()));

            var map = new Dictionary<string, InterpretePresenceDto>();

            foreach (var r in rows)
            {
                var key = r.Tolkcode.HasValue
                    ? r.Tolkcode.Value.ToString()
                    : $"{r.Nom}|{r.Prenom}|{r.Gsm}|{r.Tel}|{r.Telbis}";

                if (!map.TryGetValue(key, out var item))
                {
                    item = new InterpretePresenceDto
                    {
                        Tolkcode = r.Tolkcode,
                        Nom = r.Nom ?? "",
                        Prenom = r.Prenom ?? "",
                        Telephones = new[] { r.Gsm, r.Tel, r.Telbis }
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList(),
                        FrNl = r.Taalrol switch
                        {
                            1 => "NL",
                            2 => "FR",
                            _ => ""
                        }
                    };
                    map[key] = item;
                }

                // Chercher le magistrat via la vue calendar
                string? magistrat = null;
                if (r.Tolkcode.HasValue)
                {
                    magistratMap.TryGetValue((r.Tolkcode.Value, r.HeureAudience, r.SalleAudience), out magistrat);
                }

                item.Audiences.Add(new InterpreteAudienceDto
                {
                    Heure = r.HeureAudience,
                    Salle = r.SalleAudience,
                    Langue = r.LangueRequete,
                    Magistrat = magistrat
                });
            }

            if (map.Count > 0)
            {
                // Consolider les audiences par (Heure, Salle) et compter NbAffaires par audience
                foreach (var item in map.Values)
                {
                    item.Audiences = item.Audiences
                        .GroupBy(a => new { a.Heure, a.Salle })
                        .Select(g => new InterpreteAudienceDto
                        {
                            Heure = g.Key.Heure,
                            Salle = g.Key.Salle,
                            Langue = string.Join(", ",
                                g.Select(a => a.Langue)
                                 .Where(l => !string.IsNullOrWhiteSpace(l))
                                 .Distinct()),
                            Magistrat = string.Join(", ",
                                g.Select(a => a.Magistrat)
                                 .Where(m => !string.IsNullOrWhiteSpace(m))
                                 .Distinct()),
                            NbAffaires = g.Count()
                        })
                        .OrderBy(a => a.Heure)
                        .ThenBy(a => a.Salle)
                        .ToList();

                    item.NbAffaires = item.Audiences.Sum(a => a.NbAffaires);
                }

                return map.Values
                    .OrderBy(i => i.Audiences.FirstOrDefault()?.Heure)
                    .ThenBy(i => i.Audiences.FirstOrDefault()?.Salle)
                    .ThenBy(i => i.Nom)
                    .ThenBy(i => i.Prenom)
                    .ToList();
            }

            // b) Fallback : PRESTATION (+ TOLKLINK + VUE_CALENDAR_*)

            var prestations = await _db.Prestations.AsNoTracking()
                .Where(p => p.DatePrestation >= d0 && p.DatePrestation < d1)
                .Select(p => new { p.IdPrestation, p.Tolkcode, p.Startheure })
                .ToListAsync(ct);

            if (prestations.Count == 0)
                return new List<InterpretePresenceDto>();

            var idsPrest = prestations.Select(p => p.IdPrestation).Distinct().ToList();

            var links = await _db.Tolklinks.AsNoTracking()
                .Where(tl => tl.IdPrestation.HasValue && idsPrest.Contains(tl.IdPrestation.Value))
                .Select(tl => new { tl.IdPrestation, tl.NrAffAudience })
                .ToListAsync(ct);

            var idsAff = links.Where(l => l.NrAffAudience.HasValue)
                              .Select(l => l.NrAffAudience!.Value)
                              .Distinct()
                              .ToList();

            var details = new Dictionary<int, (string? Heure, string? Salle, string? Langue)>();
            if (idsAff.Count > 0)
            {
                var ann = await _db.VueCalendarAnns.AsNoTracking()
                    .Where(a => a.IdAffAudience != 0 && a.DateAudience >= d0 && a.DateAudience < d1
                                && idsAff.Contains((int)a.IdAffAudience))
                    .Select(a => new { Id = (int)a.IdAffAudience, a.HeureAudience, a.SalleAudience, a.LangueRequete })
                    .ToListAsync(ct);
                foreach (var a in ann)
                    details[a.Id] = (a.HeureAudience, a.SalleAudience, a.LangueRequete);

                var vrm = await _db.VueCalendarVrmPcs.AsNoTracking()
                    .Where(v => v.IdAffAudience != 0 && v.DateAudience >= d0 && v.DateAudience < d1
                                && idsAff.Contains((int)v.IdAffAudience!))
                    .Select(v => new { Id = (int)v.IdAffAudience!, v.HeureAudience, v.SalleAudience, v.LangueRequete })
                    .ToListAsync(ct);
                foreach (var v in vrm)
                    details[v.Id] = (v.HeureAudience, v.SalleAudience, v.LangueRequete);
            }

            // Identités
            var tolkInt = prestations.Select(p => int.TryParse(p.Tolkcode, out var i) ? i : 0)
                                     .Where(i => i > 0).Distinct().ToList();

            var identites = tolkInt.Count == 0
                ? new Dictionary<string, Tolkidentity>()
                : await _db.Tolkidentities.AsNoTracking()
                      .Where(t => tolkInt.Contains(t.Tolkcode))
                      .ToDictionaryAsync(t => t.Tolkcode.ToString(), ct);

            // Construction
            var byInterp = prestations.GroupBy(p => p.Tolkcode);
            var list = new List<InterpretePresenceDto>();

            foreach (var g in byInterp)
            {
                var key = g.Key;
                identites.TryGetValue(key, out var id);

                var dto = new InterpretePresenceDto
                {
                    Tolkcode = int.TryParse(key, out var k) ? k : (int?)null,
                    Nom = id?.Nom ?? "",
                    Prenom = id?.Prenom ?? "",
                    Telephones = new[] { id?.Gsm, id?.Tel, id?.Telbis }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
                    FrNl = "" // inconnu hors vue détail
                };

                // Audiences depuis TOLKLINK -> VUE_CALENDAR_*; sinon heure = Startheure
                var prestIds = g.Select(x => x.IdPrestation).ToHashSet();
                var affForPrest = links.Where(l => l.NrAffAudience.HasValue && l.IdPrestation.HasValue && prestIds.Contains(l.IdPrestation.Value))
                                       .Select(l => l.NrAffAudience!.Value)
                                       .Distinct()
                                       .ToList();

                if (affForPrest.Count > 0)
                {
                    foreach (var affId in affForPrest)
                    {
                        if (details.TryGetValue(affId, out var d))
                        {
                            dto.Audiences.Add(new InterpreteAudienceDto
                            {
                                Heure = d.Heure,
                                Salle = d.Salle,
                                Langue = d.Langue
                            });
                        }
                    }
                }

                // Si on n’a rien trouvé via les vues, mettre au moins l’heure de start
                if (dto.Audiences.Count == 0)
                {
                    var minStart = g.Min(x => x.Startheure);
                    dto.Audiences.Add(new InterpreteAudienceDto
                    {
                        Heure = minStart.ToString("HH:mm"),
                        Salle = null,
                        Langue = null
                    });
                }

                list.Add(dto);
            }

            // Consolider les audiences et calculer NbAffaires pour le fallback
            foreach (var item in list)
            {
                item.Audiences = item.Audiences
                    .GroupBy(a => new { a.Heure, a.Salle })
                    .Select(g => new InterpreteAudienceDto
                    {
                        Heure = g.Key.Heure,
                        Salle = g.Key.Salle,
                        Langue = string.Join(", ",
                            g.Select(a => a.Langue)
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .Distinct()),
                        Magistrat = string.Join(", ",
                            g.Select(a => a.Magistrat)
                             .Where(m => !string.IsNullOrWhiteSpace(m))
                             .Distinct()),
                        NbAffaires = g.Count()
                    })
                    .OrderBy(a => a.Heure)
                    .ThenBy(a => a.Salle)
                    .ToList();

                item.NbAffaires = item.Audiences.Sum(a => a.NbAffaires);
            }

            return list
                .OrderBy(i => i.Audiences.FirstOrDefault()?.Heure)
                .ThenBy(i => i.Audiences.FirstOrDefault()?.Salle)
                .ThenBy(i => i.Nom)
                .ThenBy(i => i.Prenom)
                .ToList();
        }

        // 5) PDF
        [HttpGet("interpretes/pdf")]
        public async Task<IActionResult> ExportInterpretesPdf([FromQuery] DateOnly date, CancellationToken ct = default)
        {
            var jour = date;
            var data = await GetData(jour, ct);

            var multiSalle = data
                .Where(i => i.Audiences
                    .Select(a => a.Salle)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .Skip(1).Any())
                .Select(i => i.Tolkcode)
                .Where(c => c.HasValue)
                .Select(c => c!.Value)
                .ToHashSet();

            var lignesGroupees = data
                .SelectMany(i => i.Audiences.Select(a => new { Interprete = i, Audience = a }))
                .OrderBy(x => x.Audience.Heure)
                .ThenBy(x => x.Audience.Salle)
                .ThenBy(x => x.Interprete.Nom)
                .ThenBy(x => x.Interprete.Prenom)
                .ToList();

            var sansAudience = data
                .Where(i => i.Audiences.Count == 0)
                .OrderBy(i => i.Nom)
                .ThenBy(i => i.Prenom)
                .ToList();

            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text($"Présence interprètes — {jour:yyyy-MM-dd}")
                            .FontSize(14).Bold();
                        column.Item().PaddingTop(5);
                    });

                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);   // Présent
                                columns.ConstantColumn(40);   // Heure
                                columns.ConstantColumn(35);   // Salle
                                columns.RelativeColumn(3);    // Interprète
                                columns.RelativeColumn(2);    // Téléphone
                                columns.RelativeColumn(2.5f); // Langue
                                columns.ConstantColumn(30);   // Aff.
                                columns.ConstantColumn(35);   // FR/NL
                                columns.RelativeColumn(3);    // Remarque
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Présent").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Heure").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Salle").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Interprète (#*)").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Téléphone").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Langue").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Aff.").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("FR/NL").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                    .Padding(3).AlignCenter().Text("Remarque").FontSize(9).Bold();
                            });

                            foreach (var x in lignesGroupees)
                            {
                                var i = x.Interprete;
                                bool hasMultiSalle = i.Tolkcode.HasValue && multiSalle.Contains(i.Tolkcode.Value);
                                var nom = $"{i.Nom} {i.Prenom}";
                                var numero = $"#{i.Tolkcode}{(hasMultiSalle ? "*" : "")}";

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text("☐").FontSize(10);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text(x.Audience.Heure ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text(x.Audience.Salle ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Row(row =>
                                    {
                                        row.AutoItem().Text(nom + " (");
                                        row.AutoItem().Text(numero).FontSize(10).Bold();
                                        row.AutoItem().Text(")");
                                    });
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text(string.Join(" / ", i.Telephones));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text(x.Audience.Langue ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text(x.Audience.NbAffaires.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text(i.FrNl ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text("");
                            }

                            foreach (var i in sansAudience)
                            {
                                var nom = $"{i.Nom} {i.Prenom}";
                                var numero = $"#{i.Tolkcode}";

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text("☐").FontSize(10);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text("");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text("");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Row(row =>
                                    {
                                        row.AutoItem().Text(nom + " (");
                                        row.AutoItem().Text(numero).FontSize(10).Bold();
                                        row.AutoItem().Text(")");
                                    });
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text(string.Join(" / ", i.Telephones));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text("");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text("0");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignCenter().Text(i.FrNl ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignLeft().Text("");
                            }
                        });
                    });

                    page.Footer().Column(column =>
                    {
                        column.Item().PaddingTop(5).Text("* Interprète présent dans plusieurs salles.")
                            .FontSize(8).Italic();
                    });
                });
            });

            var bytes = document.GeneratePdf();
            var fname = $"Presence_Interpretes_{jour:yyyy-MM-dd}.pdf";

            return File(bytes, "application/pdf", fname);
        }
    }
}
