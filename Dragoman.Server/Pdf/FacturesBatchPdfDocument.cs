using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Dragoman.Server.Pdf;

public record FactureRow(
    DateTime Date,
    string Debut,
    string Fin,
    int Duree,
    decimal Km,
    decimal Montant,
    decimal Transport
);

public record FactureModel(
    string Month,                 // "2025-09"
    bool IsNl,                    // true=NL, false=FR
    string SupplierName,          // "NOM PRENOM"
    string SupplierStreetLine,    // "Rue X 10 bte 2"
    string SupplierCityLine,      // "1000 Ville"
    string? VatNumber,            // "BE0xxxxxxxxx" ou string TVA
    string? Kenmerk,              // Référence (tolkcode)
    string? Bank,                 // BANKREKENING formaté (xxx-xxxxxxx-xx)
    string? Fedcom,               // FEDCOMNUMMER
    string CustomerBlock,         // bloc CCE
    IReadOnlyList<FactureRow> Rows,
    decimal TotalPrestation,
    decimal TotalDeplacement,
    decimal TotalBaseHt,
    decimal TotalTva,
    decimal TotalTtc,
    string? Reference = null,     // RVV-CCE/{id}
    string? PoNumber = null,      // PO
    string? Ondernemingsnummer = null, // Numéro d'entreprise
    bool IsNoteDeCredit = false,
    string? OriginalReference = null, // Réf facture annulée (pour note de crédit)
    byte? TvaStatutId = null // 1=TVA 21%, 2=TVA non applicable, 3=Exonéré
);

public class FacturesBatchPdfDocument : IDocument
{
    private readonly IReadOnlyList<FactureModel> _factures;

    public FacturesBatchPdfDocument(IReadOnlyList<FactureModel> factures)
        => _factures = factures;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (var f in _factures)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(40);
                page.MarginRight(40);
                page.MarginTop(50);
                page.MarginBottom(40);

                page.DefaultTextStyle(x => x.FontSize(10));

                var culture = new CultureInfo(f.IsNl ? "nl-BE" : "fr-BE");

                page.Content()
                    .PaddingVertical(20)
                    .Column(col =>
                    {
                        col.Spacing(8);

                        // ===== TITRE FACTURE / NOTE DE CRÉDIT =====
                        if (f.IsNoteDeCredit)
                        {
                            var creditTitle = f.IsNl
                                ? $"CREDITNOTA van factuur {f.OriginalReference ?? ""}"
                                : $"NOTE DE CRÉDIT de la facture {f.OriginalReference ?? ""}";
                            col.Item().Text(creditTitle)
                                .FontSize(16).Bold().FontColor("#991b1b");
                        }
                        else
                        {
                            col.Item().Text(f.IsNl ? "FACTUUR" : "FACTURE")
                                .FontSize(18).Bold().FontColor("#1e3a5f");
                        }

                        // ===== REF + N° facture + PO + Mois =====
                        col.Item().PaddingBottom(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                if (!string.IsNullOrWhiteSpace(f.Reference))
                                {
                                    c.Item().Row(lr =>
                                    {
                                        lr.AutoItem().Text(f.IsNl ? "Ref : " : "Réf : ").SemiBold();
                                        lr.AutoItem().Text(f.Reference).Bold().FontSize(11);
                                    });
                                }
                                // N° facture (champ vide à remplir manuellement)
                                c.Item().Row(lr =>
                                {
                                    lr.AutoItem().Text(f.IsNl ? "Factuurnummer : " : "N° facture : ").SemiBold();
                                    lr.AutoItem().Text("__________________").FontSize(11);
                                });
                                if (!string.IsNullOrWhiteSpace(f.Ondernemingsnummer))
                                {
                                    c.Item().Row(lr =>
                                    {
                                        lr.AutoItem().Text(f.IsNl ? "Ondernemingsnummer : " : "N° d'entreprise : ").SemiBold();
                                        lr.AutoItem().Text(f.Ondernemingsnummer).Bold().FontSize(11);
                                    });
                                }
                                if (!string.IsNullOrWhiteSpace(f.PoNumber))
                                {
                                    c.Item().Row(lr =>
                                    {
                                        lr.AutoItem().Text("PO : ").SemiBold();
                                        lr.AutoItem().Text(f.PoNumber).Bold().FontSize(11);
                                    });
                                }
                            });
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().Text(f.IsNl ? "Periode : " + f.Month : "Période : " + f.Month).SemiBold();
                            });
                        });

                        col.Item().LineHorizontal(1).LineColor("#cbd5e1");

                        // ===== HEADER : Fournisseur / Client (sans labels) =====
                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.Spacing(25);

                            r.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().Text(f.SupplierName).SemiBold();
                                if (!string.IsNullOrWhiteSpace(f.SupplierStreetLine))
                                    c.Item().Text(f.SupplierStreetLine);
                                if (!string.IsNullOrWhiteSpace(f.SupplierCityLine))
                                    c.Item().Text(f.SupplierCityLine);
                            });

                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().AlignRight().Text(f.CustomerBlock);
                            });
                        });

                        // ===== Bloc TVA / Référence / Bank / Fedcom =====
                        col.Item().PaddingTop(14).Row(r =>
                        {
                            r.Spacing(25);

                            r.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                if (!string.IsNullOrWhiteSpace(f.VatNumber))
                                {
                                    c.Item().Text(f.IsNl ? "BTW-nummer:" : "N° TVA :").SemiBold();
                                    c.Item().Text(f.VatNumber).SemiBold();
                                }
                            });

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Row(x =>
                                {
                                    x.Spacing(15);

                                    x.AutoItem().MinWidth(60).Column(cc =>
                                    {
                                        cc.Spacing(2);
                                        cc.Item().Text(f.IsNl ? "Kenmerk" : "Référence").SemiBold().FontSize(8).FontColor("#94a3b8");
                                        cc.Item().Text(f.Kenmerk ?? "");
                                    });

                                    x.RelativeItem(2).Column(cc =>
                                    {
                                        cc.Spacing(2);
                                        cc.Item().Text(f.IsNl ? "Bankrekening" : "Compte bancaire").SemiBold().FontSize(8).FontColor("#94a3b8");
                                        cc.Item().Text(f.Bank ?? "").FontSize(10);
                                    });

                                    x.AutoItem().MinWidth(60).Column(cc =>
                                    {
                                        cc.Spacing(2);
                                        cc.Item().Text("Fedcom").SemiBold().FontSize(8).FontColor("#94a3b8");
                                        cc.Item().Text(f.Fedcom ?? "");
                                    });
                                });
                            });
                        });

                        // ===== TABLE =====
                        col.Item().PaddingTop(20).Table(t =>
                        {
                            t.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                void HeaderCell(IContainer c, string text) =>
                                    c.BorderBottom(1).BorderColor("#94a3b8").PaddingBottom(4)
                                        .Text(text).SemiBold().FontSize(9).FontColor("#334155");

                                HeaderCell(h.Cell(), f.IsNl ? "Datum" : "Date");
                                HeaderCell(h.Cell(), f.IsNl ? "Begin" : "Début");
                                HeaderCell(h.Cell(), f.IsNl ? "Einde" : "Fin");
                                h.Cell().BorderBottom(1).BorderColor("#94a3b8").PaddingBottom(4).AlignRight()
                                    .Text(f.IsNl ? "Duur" : "Durée").SemiBold().FontSize(9).FontColor("#334155");
                                h.Cell().BorderBottom(1).BorderColor("#94a3b8").PaddingBottom(4).AlignRight()
                                    .Text("Km").SemiBold().FontSize(9).FontColor("#334155");
                                h.Cell().BorderBottom(1).BorderColor("#94a3b8").PaddingBottom(4).AlignRight()
                                    .Text(f.IsNl ? "€ Prestatie" : "€ prestation").SemiBold().FontSize(9).FontColor("#334155");
                                h.Cell().BorderBottom(1).BorderColor("#94a3b8").PaddingBottom(4).AlignRight()
                                    .Text(f.IsNl ? "€ Verplaatsing" : "€ déplacement").SemiBold().FontSize(9).FontColor("#334155");
                            });

                            foreach (var row in f.Rows)
                            {
                                t.Cell().PaddingVertical(2).Text(row.Date.ToString("dd/MM/yyyy"));
                                t.Cell().PaddingVertical(2).Text(row.Debut);
                                t.Cell().PaddingVertical(2).Text(row.Fin);
                                t.Cell().PaddingVertical(2).AlignRight().Text(row.Duree.ToString(culture));
                                t.Cell().PaddingVertical(2).AlignRight().Text(row.Km.ToString("0", culture));
                                t.Cell().PaddingVertical(2).AlignRight().Text(row.Montant.ToString("N2", culture));
                                t.Cell().PaddingVertical(2).AlignRight().Text(row.Transport.ToString("N2", culture));
                            }
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#cbd5e1");

                        // ===== TOTALS =====
                        var showExclBtw = f.TvaStatutId is null or 1;
                        var showTvaLine = showExclBtw && f.TotalTva > 0;

                        col.Item().PaddingTop(6).AlignRight().Column(c =>
                        {
                            c.Spacing(3);

                            c.Item().Row(r =>
                            {
                                var lbl = showExclBtw
                                    ? (f.IsNl ? "Totaal prestatie excl. BTW" : "Total prestation")
                                    : (f.IsNl ? "Totaal prestatie" : "Total prestation");
                                r.RelativeItem().Text(lbl).FontSize(9);
                                r.ConstantItem(110).AlignRight().Text(f.TotalPrestation.ToString("N2", culture));
                            });

                            c.Item().Row(r =>
                            {
                                var lbl = showExclBtw
                                    ? (f.IsNl ? "Totaal verplaatsing excl. BTW" : "Total déplacement")
                                    : (f.IsNl ? "Totaal verplaatsing" : "Total déplacement");
                                r.RelativeItem().Text(lbl).FontSize(9);
                                r.ConstantItem(110).AlignRight().Text(f.TotalDeplacement.ToString("N2", culture));
                            });

                            c.Item().PaddingTop(4).Row(r =>
                            {
                                var lbl = showExclBtw
                                    ? (f.IsNl ? "Totaal excl. BTW" : "Total HT")
                                    : (f.IsNl ? "Totaal" : "Total");
                                r.RelativeItem().Text(lbl).SemiBold();
                                r.ConstantItem(110).AlignRight().Text(f.TotalBaseHt.ToString("N2", culture)).SemiBold();
                            });

                            if (showTvaLine)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(f.IsNl ? "BTW 21%" : "TVA 21%").FontSize(9);
                                    r.ConstantItem(110).AlignRight().Text(f.TotalTva.ToString("N2", culture));
                                });

                                c.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text(f.IsNl ? "Totaal incl. BTW" : "Total TTC").SemiBold().FontSize(12);
                                    r.ConstantItem(110).AlignRight().Text(f.TotalTtc.ToString("N2", culture)).SemiBold().FontSize(12);
                                });
                            }
                            else if (showExclBtw)
                            {
                                c.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text(f.IsNl ? "Totaal" : "Total").SemiBold().FontSize(12);
                                    r.ConstantItem(110).AlignRight().Text(f.TotalTtc.ToString("N2", culture)).SemiBold().FontSize(12);
                                });
                            }
                        });

                        // Disclaimer for TVA statut 2 (kleine onderneming / TVA non applicable)
                        if (f.TvaStatutId == 2)
                        {
                            col.Item().PaddingTop(16).Text(f.IsNl
                                ? "Kleine onderneming onderworpen aan de vrijstellingsregeling van belasting. BTW niet toepasselijk."
                                : "Entreprise dont TVA non applicable.")
                                .FontSize(9).Italic().FontColor("#64748b");
                        }

                        col.Item().PaddingTop(30).Text(f.IsNl ? "Datum en handtekening" : "Date et signature");
                    });
            });
        }
    }
}
