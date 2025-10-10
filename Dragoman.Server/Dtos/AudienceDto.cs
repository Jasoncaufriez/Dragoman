namespace Dragoman.Server.Dtos
{
    public class AudienceDto
    {
        public decimal NroRoleGen { get; set; }
        public string LangueRole { get; set; } = "";
        public string Proc { get; set; } = "";
        public DateTime DateAudience { get; set; }
        public string Nom { get; set; } = "";
        public string SalleAudience { get; set; } = "";
        public string HeureAudience { get; set; } = "";
        public string LangueRequete { get; set; } = "";
        public string LibelleFr { get; set; } = "";
        public string LangueCgoe { get; set; } = "";
        public decimal IdAffAudience { get; set; }
        public decimal? Tolkcode { get; set; }
    }
}
