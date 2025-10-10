using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dragoman.Server.Models;

[Table("TOLKIDENTITY")]
public partial class Tolkidentity
{
    [Key]
    [Column("TOLKCODE")]
    public int Tolkcode { get; set; }

    [Column("TAALROL")]
    public int? Taalrol { get; set; } // 1=FR, 2=NL

    [Column("NOM"), StringLength(50)]
    public string? Nom { get; set; }

    [Column("PRENOM"), StringLength(50)]
    public string? Prenom { get; set; }

    // Adresse — mappée côté modèle, pas utilisée dans l’UI (à ta demande)
    [Column("RUE"), StringLength(50)]
    public string? Rue { get; set; }

    [Column("ADRESNR"), StringLength(50)]
    public string? Adresnr { get; set; }

    [Column("ADRESBUSNR"), StringLength(50)]
    public string? Adresbusnr { get; set; }

    [Column("POSTID"), StringLength(12)]
    public string? Postid { get; set; }

    [Column("TEL"), StringLength(50)]
    public string? Tel { get; set; }

    [Column("TELBIS"), StringLength(50)]
    public string? Telbis { get; set; }

    [Column("GSM"), StringLength(50)]
    public string? Gsm { get; set; }

    [Column("FAX"), StringLength(50)]
    public string? Fax { get; set; }

    [Column("BEEDIGD")]
    public int? Beedigd { get; set; } // 1=assermenté, 0=non

    [Column("DATE_NAISSANCE")]
    public DateTime? DateNaissance { get; set; }

    [Column("NATIONALITEIT"), StringLength(50)]
    public string? Nationaliteit { get; set; }

    [Column("RIJKSREGISTERNR"), StringLength(50)]
    public string? Rijksregisternr { get; set; }

    [Column("OPENBARE_VEILIGHEID")]
    public int? OpenbareVeiligheid { get; set; }

    [Column("HERKOMST"), StringLength(20)]
    public string? Herkomst { get; set; }

    [Column("GENRE"), StringLength(1)]
    public string? Genre { get; set; }

    [Column("BEROEPSCODE")]
    public int? Beroepscode { get; set; }

    [Column("BTW_NR")]
    public int? BtwNr { get; set; }

    [Column("BANKREKENING"), StringLength(20)]
    public string? Bankrekening { get; set; }

    [Column("REMARQUE"), StringLength(250)]
    public string? Remarque { get; set; }

    [Column("EVALUATIECODE")]
    public int? Evaluatiecode { get; set; }

    [Column("BA"), StringLength(11)]
    public string? Ba { get; set; }

    [Column("FEDCOM")]
    public int? Fedcom { get; set; }

    [Column("ONDERNEMINGSNUMMER")]
    public int? Ondernemingsnummer { get; set; }

    [Column("VESTIGINGSNUMMER"), StringLength(10)]
    public string? Vestigingsnummer { get; set; }

    [Column("FEDCOMNUMMER")]
    public int? Fedcomnummer { get; set; }

    [Column("LANGUE_ADMINISTRATIVE"), StringLength(20)]
    public string? LangueAdministrative { get; set; } // doublon historique,  ignorer côté UI

    [Column("EMAIL"), StringLength(80)]
    public string? Email { get; set; }

    [Column("IBAN"), StringLength(34)]
    public string? Iban { get; set; }

    [Column("TVA"), StringLength(20)]
    public string? Tva { get; set; }

    [Column("ISCCE"), StringLength(1)]
    public string? Iscce { get; set; }

  //  public ICollection<Tolkadresse> Adresses { get; set; } = new List<Tolkadresse>();

}
