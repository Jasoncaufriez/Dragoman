namespace Dragoman.Server.Dtos
{
    public class InterpreteAudienceDto
    {
        public string? Heure { get; set; }
        public string? Salle { get; set; }
        public string? Langue { get; set; }
        public string? Magistrat { get; set; }
        public int NbAffaires { get; set; }
    }

    public class InterpretePresenceDto
    {
        public int? Tolkcode { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public List<string> Telephones { get; set; } = new(); // GSM, TEL, TELBIS séparés

        // FR / NL dérivé de TAALROL
        public string? FrNl { get; set; }

        public int NbAffaires { get; set; }

        public List<InterpreteAudienceDto> Audiences { get; set; } = new();
    }
}
