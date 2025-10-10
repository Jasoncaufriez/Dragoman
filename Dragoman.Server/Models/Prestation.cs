using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Prestation
{
    public int IdPrestation { get; set; }
    public string Tolkcode { get; set; } = null!;
    public DateTime DatePrestation { get; set; }
    public DateTime Startheure { get; set; }
    public DateTime Endheure { get; set; }
    public string? UserCreate { get; set; }
    public int IdPaiement { get; set; }

    public virtual Paiement? IdPaiementNavigation { get; set; }

    public virtual ICollection<Tolklink> Tolklinks { get; set; } = new List<Tolklink>();
}
