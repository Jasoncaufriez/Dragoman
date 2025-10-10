namespace Dragoman.Server.Dtos;

public class PrestationJourRowDto
{
    public string Tolkcode { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? Telephone { get; set; }
    public int[] IdAffAudiences { get; set; } = Array.Empty<int>();
    public string? HeureAudienceSuggee { get; set; }
    public bool HasPrestation { get; set; }
}

public class NewPrestationDto
{
    public string Tolkcode { get; set; } = "";
    public DateTime DatePrestation { get; set; }
    public DateTime Startheure { get; set; }
    public DateTime Endheure { get; set; }
    public int[] IdAffAudiences { get; set; } = Array.Empty<int>();
}

public class AbsenceDto
{
    public string Tolkcode { get; set; } = "";
    public int IdAffAudience { get; set; }
    public DateTime DatePrestation { get; set; }
}

public class RemplacementDto
{
    public int IdAffAudience { get; set; }
    public string AncienTolkcode { get; set; } = "";
    public string NouveauTolkcode { get; set; } = "";
}