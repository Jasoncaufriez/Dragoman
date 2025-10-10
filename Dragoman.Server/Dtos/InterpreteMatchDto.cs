namespace Dragoman.Server.Dtos;

public class InterpreteMatchDto
{
    public int Tolkcode { get; set; }
    public string? Nom { get; set; }
    public string? Prenom { get; set; }

    // Téléphones (issus de Tolkidentity)
    public string? Tel { get; set; }
    public string? Telbis { get; set; }
    public string? Gsm { get; set; }

    // Liste des langues destination (utile pour l’UI)
    public List<string> LanguesDestination { get; set; } = new();

    // Distance (KM) depuis l’adresse active (TOLKADRESSE.ENDDATE IS NULL)
    public double? DistanceKm { get; set; }
}
