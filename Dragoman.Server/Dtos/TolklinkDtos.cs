namespace Dragoman.Server.Dtos;

public class TolklinkDto
{
    public int IdTolklink { get; set; }
    public int? NrAffAudience { get; set; }
    public int Tolkcode { get; set; }
    public DateTime Datecreate { get; set; }
    public DateTime? Datesupp { get; set; }
    public int? IdPrestation { get; set; }
}

public class NewTolklinkDto
{
    public int NrAffAudience { get; set; }
}

public class BulkNewTolklinkDto
{
    public int[] Ids { get; set; } = Array.Empty<int>();
}
