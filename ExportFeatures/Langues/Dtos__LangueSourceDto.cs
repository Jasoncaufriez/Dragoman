namespace Dragoman.Server.Dtos
{
    public class LangueSourceDto
    {
        public int IdLangueSource { get; set; }
        public int Tolkcode { get; set; }
        public int NrLangue { get; set; }   // correspond à l’ID de la langue
        public string? LibelleFr { get; set; } // jointure avec Langue
        public string? LibelleNl { get; set; }
    }
}
