using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.DTOs
{
    public class PaiementValidationDto
    {
        [Required]
        public string ComptePaiement { get; set; }

        [Required]
        public string MontantEnLettres { get; set; }

        [Required]
        public string EffectuePar { get; set; }

        public string? Statut { get; set; }



        public List<DemandeItemPriceDto> DemandeItems { get; set; } = new List<DemandeItemPriceDto>();

    }
    public class DemandeItemPriceDto
    {
        public Guid Id { get; set; }
        public decimal? PrixUnitaire { get; set; }
    }

}
