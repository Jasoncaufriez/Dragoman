

namespace Dragoman.Server.Dtos
{
    public class InterpreteAudienceDto
    {
        public string? Heure { get; set; }
        public string? Salle { get; set; }
        public string? Langue { get; set; }
    }
    public class InterpretePresenceDto
    {
        public int? Tolkcode { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Telephone { get; set; } // concat GSM/TEL/TELBIS
        public List<InterpreteAudienceDto> Audiences { get; set; } = new();
    }
}
