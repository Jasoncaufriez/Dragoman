using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class VueTva
{
    public int Tolkcode { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? TypeStatut { get; set; }
}
