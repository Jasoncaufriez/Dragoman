namespace Dragoman.Server.Dtos
{
    public class TvaRowDto
    {
        public int IdTva { get; set; }
        public int Tolkcode { get; set; }
        public byte IdStatut { get; set; }
        public string Statut { get; set; } = "";   // libellé du statut
        public DateTime? Startdate { get; set; }
        public DateTime? Enddate { get; set; }
    }

    public class NewTvaDto
    {
        public byte IdStatut { get; set; }
        public DateTime Startdate { get; set; }    // date de début du nouveau statut
    }

    public class StatutDto
    {
        public byte IdStatut { get; set; }
        public string Libelle { get; set; } = "";
    }
}
