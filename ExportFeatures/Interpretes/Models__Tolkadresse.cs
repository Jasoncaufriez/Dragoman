using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dragoman.Server.Models;

[Table("TOLKADRESSE")]
public partial class Tolkadresse
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("ID_ADRESSE")]
    public int IdAdresse { get; set; }

    [ValidateNever]
    [BindNever]
    [Column("TOLKCODE")]
    public string Tolkcode { get; set; } = null!;


    [Column("LAND")]
    [Required]
    public string Land { get; set; } = null!;

    [Column("CP")]
    [Required]
    public string Cp { get; set; } = null!;

    [Column("COMMUNE")]
    [Required]
    public string Commune { get; set; } = null!;

    [Column("RUE")]
    public string? Rue { get; set; }

    [Column("NUMERO")]
    public string? Numero { get; set; }

    [Column("BOITE")]
    public string? Boite { get; set; }

    [Column("KM")]
    public byte? Km { get; set; }

    [Column("STARTDATE")]
    public DateTime Startdate { get; set; }

    [Column("ENDDATE")]
    public DateTime? Enddate { get; set; }

    [Column("DATECREATE")]
    public DateTime Datecreate { get; set; }

    [Column("USERCREATE")]
    public string? Usercreate { get; set; }

    [Column("DATEMODIF")]
    public DateTime? Datemodif { get; set; }

    [Column("USERMODIF")]
    public string? Usermodif { get; set; }

    // --------- Navigation vers TOLKIDENTITY ----------
  //  [ForeignKey(nameof(Tolkcode))]
   // public virtual Tolkidentity? Tolk { get; set; }
}
