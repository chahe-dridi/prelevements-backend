using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public enum StatutDemande
    {
        EnAttente,
        Validee,
        Refusee
    }

    public class Demande
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UtilisateurId { get; set; }
        public User Utilisateur { get; set; }

        public DateTime DateDemande { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid CategorieId { get; set; }
        public Categorie Categorie { get; set; }

        public StatutDemande Statut { get; set; } = StatutDemande.EnAttente;

        public ICollection<DemandeItem> DemandeItems { get; set; } = new List<DemandeItem>();

        public Paiement Paiement { get; set; }
    }
}
