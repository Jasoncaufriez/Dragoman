namespace Dragoman.Server.Dtos;

public class PaiementMoisInterpreteRowDto
{
    public string Tolkcode { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";

    // 1=FR, 2=NL (vient de TOLKIDENTITY.TAALROL)
    public int? Taalrol { get; set; }

    public int NbPrestations { get; set; }

    public decimal Montant { get; set; }
    public decimal Transport { get; set; }
    public decimal MontantTva { get; set; }
    public decimal Total { get; set; }

    public int? IdFacture { get; set; }
}

public class PaiementMoisDetailRowDto
{
    public int IdPaiement { get; set; }
    public DateTime Date { get; set; }
    public string Debut { get; set; } = "";
    public string Fin { get; set; } = "";
    public int Duree { get; set; }          // minutes
    public decimal Km { get; set; }         // km aller
    public decimal Montant { get; set; }
    public decimal Transport { get; set; }

    public int? IdFacture { get; set; }
}

public class PaiementMoisTotauxDto
{
    public decimal Montant { get; set; }
    public decimal Transport { get; set; }
    public decimal BaseHt { get; set; }
    public decimal MontantTva { get; set; }
    public decimal Total { get; set; }
}

public class PaiementMoisDetailDto
{
    public string Tolkcode { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";

    // 1=FR, 2=NL
    public int? Taalrol { get; set; }

    public List<PaiementMoisDetailRowDto> Rows { get; set; } = new();
    public PaiementMoisTotauxDto Totaux { get; set; } = new();
}
