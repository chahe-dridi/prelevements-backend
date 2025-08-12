using System;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public class Paiement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DemandeId { get; set; }
        public Demande Demande { get; set; }

        public DateTime DatePaiement { get; set; } = DateTime.UtcNow;

        [Required]
        public string EffectuePar { get; set; }

        [Required]
        public string ComptePaiement { get; set; }

        [Required]
        public decimal MontantTotal { get; set; }

        [Required]
        public string MontantEnLettres { get; set; }
    }
}
