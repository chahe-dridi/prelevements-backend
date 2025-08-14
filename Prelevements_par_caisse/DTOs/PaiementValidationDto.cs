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


    }


}
