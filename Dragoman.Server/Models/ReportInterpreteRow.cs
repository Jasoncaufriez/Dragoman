using System.ComponentModel.DataAnnotations.Schema;

[Table("V_REPORT_INTERPRETE")] // <- mets le vrai nom (vue/table)
public class ReportInterpreteRow
{
    [Column("TOLKCODE")] public int? Tolkcode { get; set; }

    [Column("NOM")] public string? Nom { get; set; }

    [Column("PRENOM")] public string? Prenom { get; set; }
    [Column("JOUR")] public DateTime? Jour { get; set; }

    [Column("HEURE_AUDIENCE")] public string? HeureAudience { get; set; }

    [Column("SALLE_AUDIENCE")] public string? SalleAudience { get; set; }

    [Column("LANGUE_REQUETE")] public string? LangueRequete { get; set; }

    [Column("GSM")] public string? Gsm { get; set; }

    [Column("TEL")] public string? Tel { get; set; }

    [Column("TELBIS")] public string? Telbis { get; set; }
}
