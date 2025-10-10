using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Models;

[Keyless]
[Table("VUE_CALENDAR_VRM_PC")]
public partial class VueCalendarVrmPc
{
    [Column("NRO_ROLE_GEN")]
    public decimal NroRoleGen { get; set; }

    [Column("LANGUE_ROLE")]
    public string? LangueRole { get; set; }

    [Column("PROC")]
    public string? Proc { get; set; }

    [Column("DATE_AUDIENCE")]
    public DateTime? DateAudience { get; set; }

    [Column("NOM")]
    public string Nom { get; set; } = null!;

    [Column("SALLE_AUDIENCE")]
    public string? SalleAudience { get; set; }

    [Column("HEURE_AUDIENCE")]
    public string? HeureAudience { get; set; }

    [Column("LANGUE_REQUETE")]
    public string? LangueRequete { get; set; }

    [Column("LIBELLE_FR")]
    public string? LibelleFr { get; set; }

    [Column("LANGUE_CGOE")]
    public string? LangueCgoe { get; set; }

    [Column("ID_AFF_AUDIENCE")]
    public decimal IdAffAudience { get; set; }

    [Column("TOLKCODE")]
    public decimal? Tolkcode { get; set; }
}
