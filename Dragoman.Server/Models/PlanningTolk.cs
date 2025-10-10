using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class PlanningTolk
{
    public decimal? Tolkcode { get; set; }

    public DateTime? DateAudience { get; set; }

    public string? LangueRequete { get; set; }

    public string? LangueRole { get; set; }

    public string? Proc { get; set; }

    public string Nom { get; set; } = null!;

    public string? SalleAudience { get; set; }

    public string? HeureAudience { get; set; }

    public string? VerzoekerStatus { get; set; }

    public string? ZaakStatus { get; set; }

    public decimal IdAffAudience { get; set; }
}
