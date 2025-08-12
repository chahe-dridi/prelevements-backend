using Microsoft.AspNetCore.Mvc;

namespace Prelevements_par_caisse.DTOs
{
    public class PaiementDto
    {
        public Guid DemandeId { get; set; }
        public string EffectuePar { get; set; }
        public string ComptePaiement { get; set; }
        public decimal MontantTotal { get; set; }
        public string MontantEnLettres { get; set; }
    }
}
