using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class TolkTva
{
    public int IdTolkTva { get; set; }

    public byte IdStatut { get; set; }

    public int Tolkcode { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
