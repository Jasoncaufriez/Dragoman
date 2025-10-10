using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dragoman.Server.Models;

public partial class Langue
{
    [Key]
    [Column("IDLANGUE")]
    public byte Idlangue { get; set; }

    public string? LibelleFr { get; set; }

    public string? LibelleNl { get; set; }

    public string? CodeIso { get; set; }

    public string? TypeLangue { get; set; }

    public bool? IslangueDestination { get; set; }
}
