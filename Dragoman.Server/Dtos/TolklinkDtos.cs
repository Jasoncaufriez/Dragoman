namespace Dragoman.Server.Dtos;

public class TolklinkDto
{
    public decimal IdTolklink { get; set; }
    public decimal? NrAffAudience { get; set; }
    public decimal? Tolkcode { get; set; }
    public DateTime Datecreate { get; set; }
    public DateTime? Datemodif { get; set; }
    public DateTime? Datesupp { get; set; }
    public string? Usercreate { get; set; }
    public decimal? IdPrestation { get; set; }
}

public class CreateTolklinkDto
{
    public decimal? NrAffAudience { get; set; }
    public decimal? Tolkcode { get; set; }
    public string? Usercreate { get; set; }
}

public class UpdateTolklinkDto
{
    public decimal? NrAffAudience { get; set; }
    public decimal? Tolkcode { get; set; }
    public decimal? IdPrestation { get; set; }
}