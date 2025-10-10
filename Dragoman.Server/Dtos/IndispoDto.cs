namespace Dragoman.Server.Dtos
{
    public class IndispoDto
    {
        public short IdIndispo { get; set; }
        public string Tolkcode { get; set; } = "";
        public DateTime Startindispo { get; set; }
        public DateTime? Endindispo { get; set; }
        public string? Motifindispo { get; set; }
        public string? Commentaire { get; set; }
    }

    

    // Pour l’ajout: même noms (Startindispo, Endindispo, …) mais sans Id/Tolkcode
    public class NewIndispoDto
    {
        public DateTime Startindispo { get; set; }
        public DateTime? Endindispo { get; set; }
        public string? Motifindispo { get; set; }
        public string? Commentaire { get; set; }

        // Audit (envoyé par le front)
        public string? Createuser { get; set; }
        public string? Modifuser { get; set; }
    }

    public class UpdateIndispoDto
    {
        public DateTime? Startindispo { get; set; }  
        public DateTime? Endindispo { get; set; }
        public string? Motifindispo { get; set; }
        public string? Commentaire { get; set; }

        // Audit
        public string? Modifuser { get; set; }
    }


}
