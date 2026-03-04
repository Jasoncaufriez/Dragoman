using System;
using System.Collections.Generic;

namespace Dragoman.Server.Models;

public partial class Facture
{
    public int IdFacture { get; set; }
    public string Tolkcode { get; set; } = string.Empty;
    public DateTime DateGeneration { get; set; }
    public DateTime? DateValidationFedcom { get; set; }
    public DateTime? DateTransmission { get; set; }
    public string StatutFacture { get; set; } = "GENEREE";
    public decimal TotalTtc { get; set; }
    public int? IdFactureOrigine { get; set; }

    public virtual ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();
}