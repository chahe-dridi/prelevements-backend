using System;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public class DemandeItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DemandeId { get; set; }
        public Demande Demande { get; set; }

        [Required]
        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        [Required]
        public int Quantite { get; set; }



        public decimal? PrixUnitaire { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

    }
}
