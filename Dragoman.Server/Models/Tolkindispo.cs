using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Tolkindispo
{
    public short IdIndispo { get; set; }

    public string Tolkcode { get; set; } = null!;

    public DateTime Startindispo { get; set; }

    public DateTime? Endindispo { get; set; }

    public string? Motifindispo { get; set; }

    public string? Commentaire { get; set; }

    public DateTime Datecreate { get; set; }

    public string? Usercreate { get; set; }

    public DateTime? Datemodif { get; set; }

    public string? Usermodif { get; set; }
}
