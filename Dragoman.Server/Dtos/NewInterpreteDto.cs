namespace Dragoman.Server.Dtos;

public class NewInterpreteDto
{
    public string Nom { get; set; } = "";
    public string? Prenom { get; set; }
    public string? Email { get; set; }
    public string? Tel { get; set; }
    public string? Telbis { get; set; }
    public string? Gsm { get; set; }
    public string? Tva { get; set; }
    public string? Iban { get; set; }
    public string? Bankrekening { get; set; }
    public int? Taalrol { get; set; }
    public int? Beedigd { get; set; }
    public string? Genre { get; set; }
}
