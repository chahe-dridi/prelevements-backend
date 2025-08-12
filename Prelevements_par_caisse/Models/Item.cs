using System;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public class Item
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nom { get; set; }

        [Required]
        public decimal PrixUnitaire { get; set; }

        [Required]
        public Guid CategorieId { get; set; }
        public Categorie Categorie { get; set; }





    }
}
