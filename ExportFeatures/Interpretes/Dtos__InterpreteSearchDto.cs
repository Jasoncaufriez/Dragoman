namespace Dragoman.Server.Dtos
{
    public class InterpreteSearchDto
    {
        public string Tolkcode { get; set; } = "";
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public List<string> LanguesDestination { get; set; } = new();
        public List<string> LanguesSource { get; set; } = new();
    }
}
