using System;
using Prelevements_par_caisse.Models; // For UserRole enum

namespace Prelevements_par_caisse.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }

    public class UpdateProfileDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
    }

    public class ChangeRoleDto
    {
        public Guid UserId { get; set; }
        public UserRole Role { get; set; }
    }



    public class UpdateUserDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public UserRole? Role { get; set; } // nullable so role is optional
    }












}
