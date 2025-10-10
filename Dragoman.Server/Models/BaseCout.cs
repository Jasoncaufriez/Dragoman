using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class BaseCout
{
    public decimal? Tolkcode { get; set; }

    public DateTime? DatePrestation { get; set; }

    public decimal? KmAPayer { get; set; }

    public decimal? Testround { get; set; }

    public decimal Euroheure { get; set; }

    public decimal Eurokm { get; set; }

    public decimal? EurokmAPayer { get; set; }

    public decimal? Kmeuro { get; set; }
}
