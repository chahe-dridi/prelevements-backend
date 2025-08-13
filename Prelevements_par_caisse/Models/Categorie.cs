using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public class Categorie
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nom { get; set; }

        public string Description { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<Demande> Demandes { get; set; } = new List<Demande>();
    }
}
