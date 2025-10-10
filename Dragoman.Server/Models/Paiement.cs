using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Paiement
{
    public int IdPaiement { get; set; }
    public string Tolkcode { get; set; } = null!;
    public DateTime DatePrestation { get; set; }
    public decimal? Montant { get; set; }
    public decimal? Transport { get; set; }
    public decimal? Total { get; set; }
    public decimal? MontantTva { get; set; }
}
