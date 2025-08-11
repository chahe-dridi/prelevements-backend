using System.ComponentModel.DataAnnotations;

namespace Prelevements_par_caisse.DTOs
{
    public class RegisterRequest
    {
        [Required] public string Nom { get; set; }
        [Required] public string Prenom { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }

    public class LoginRequest
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }
}
