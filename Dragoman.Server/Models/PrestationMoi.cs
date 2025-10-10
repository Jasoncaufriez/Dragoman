using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class PrestationMoi
{
    public decimal IdPrestation { get; set; }

    public decimal? Tolkcode { get; set; }

    public DateTime? DateAudience { get; set; }

    public string? Debut { get; set; }

    public string? Fin { get; set; }

    public decimal IdPaiement { get; set; }

    public string? HeureAudience { get; set; }

    public decimal? NrAudience { get; set; }
}
