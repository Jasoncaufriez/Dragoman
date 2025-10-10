using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class CountExp
{
    public string IdCount { get; set; } = null!;

    public string? Login { get; set; }

    public DateTime? DateClic { get; set; }

    public string? TypeConvoc { get; set; }
}
