using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Indexation
{
    public byte IdIndex { get; set; }

    public DateTime Startdate { get; set; }

    public DateTime? Enddate { get; set; }

    public decimal Euro75min { get; set; }

    public decimal Euroheure { get; set; }

    public decimal Eurokm { get; set; }
}
