using System;
using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.Models
{
    public enum UserRole
    {
        SuperAdmin,
        Admin,
        Utilisateur
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Prenom { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Hashed password stored here

        [Required]
        public UserRole Role { get; set; } = UserRole.Utilisateur;




        public ICollection<Demande> Demandes { get; set; } = new List<Demande>();



    }
}
