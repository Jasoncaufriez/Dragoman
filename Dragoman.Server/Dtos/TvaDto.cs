namespace Dragoman.Server.Dtos
{
    public class TvaDto
    {
        public int IdTva { get; set; }        // mappe sur IdTolkTva
        public int Tolkcode { get; set; }     // mappe sur Tolkcode
        public byte IdStatut { get; set; }    // mappe sur IdStatut
        public DateTime? Startdate { get; set; }
        public DateTime? Enddate { get; set; }
    }
}