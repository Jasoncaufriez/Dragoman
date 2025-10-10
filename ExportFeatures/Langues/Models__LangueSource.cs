using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class LangueSource
{
    public int IdLanguesource { get; set; }

    public int Tolkcode { get; set; }

    public int? NrLangue { get; set; }

    public string? TaalcodeOld { get; set; }
}
