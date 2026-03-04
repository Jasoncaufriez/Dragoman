using System;

namespace Dragoman.Server.Models;

public partial class Tolklink
{
    // PK — NUMBER côté Oracle, on utilise int en C#
    public int IdTolklink { get; set; }

    // Colonnes simples
    public int? NrAffAudience { get; set; }       // NR_AFF_AUDIENCE
    public int? Tolkcode { get; set; }            // TOLKCODE (si ta DB stocke NUMBER)
    public DateTime Datecreate { get; set; }
    public DateTime? Datemodif { get; set; }
    public DateTime? Datesupp { get; set; }
    public string? Usercreate { get; set; }

    // FK vers PRESTATION — **type aligné** sur Prestation.IdPrestation (int)
    public int? IdPrestation { get; set; }
}
