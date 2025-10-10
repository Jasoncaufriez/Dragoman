using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dragoman.Server.Models;

public partial class Test
{
    [Column("ID")]
    public decimal Id { get; set; }
    [Column("TEST_NB")]
    public decimal? TestNb { get; set; }
    [Column("TEST_VARCHAR")]

    public string? TestVarchar { get; set; }
}
