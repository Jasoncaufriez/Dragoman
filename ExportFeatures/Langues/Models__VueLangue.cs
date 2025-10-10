using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class VueLangue
{
    public decimal Idlangue { get; set; }

    public string? LibelleFr { get; set; }

    public string? LibelleNl { get; set; }

    public string? CodeIso { get; set; }

    public string? IslangueDestination { get; set; }
}
