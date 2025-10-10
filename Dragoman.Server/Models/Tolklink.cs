using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Tolklink
{
    public decimal IdTolklink { get; set; }

    public decimal? NrAffAudience { get; set; }

    public decimal? Tolkcode { get; set; }

    public DateTime Datecreate { get; set; }

    public DateTime? Datemodif { get; set; }

    public DateTime? Datesupp { get; set; }

    public string? Usercreate { get; set; }

    public decimal? IdPrestation { get; set; }

    public virtual Prestation? IdPrestationNavigation { get; set; }
}
