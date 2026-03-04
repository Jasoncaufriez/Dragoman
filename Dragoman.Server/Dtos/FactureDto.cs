namespace Dragoman.Server.Dtos;

public class FactureDto
{
    public int IdFacture { get; set; }
    public string Tolkcode { get; set; } = "";
    public string Reference { get; set; } = "";
    public DateTime DateGeneration { get; set; }
    public DateTime? DateValidationFedcom { get; set; }
    public string StatutFacture { get; set; } = "";
    public decimal TotalTtc { get; set; }
    public int NbPaiements { get; set; }
}

public class FactureListItemDto
{
    public int IdFacture { get; set; }
    public string Reference { get; set; } = "";
    public string Tolkcode { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public DateTime DateGeneration { get; set; }
    public DateTime? DateValidationFedcom { get; set; }
    public DateTime? DateTransmission { get; set; }
    public string StatutFacture { get; set; } = "";
    public decimal TotalTtc { get; set; }
    public int? NbPaiements { get; set; }
}

public class GenererFacturesRequestDto
{
    public int Annee { get; set; }
    public int Mois { get; set; }
    public string? DateDebut { get; set; }  // "YYYY-MM-DD" — si renseigné, génère par période
    public string? DateFin { get; set; }    // "YYYY-MM-DD"
}
