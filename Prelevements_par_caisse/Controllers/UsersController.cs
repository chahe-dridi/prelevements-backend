/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Data;
using Prelevements_par_caisse.DTOs;
using Prelevements_par_caisse.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Prelevements_par_caisse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Nom = u.Nom,
                        Prenom = u.Prenom,
                        Email = u.Email,
                        Role = u.Role
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du chargement des utilisateurs", error = ex.Message });
            }
        }

        // GET: api/Users/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            var userDto = new UserDto
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Role = user.Role
            };

            return Ok(userDto);
        }

        // PUT: api/Users/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom;
            if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/role
        [HttpPut("role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound();

            user.Role = dto.Role;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Utilisateur introuvable" });

                // Validate email uniqueness if email is being changed
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != id);

                    if (emailExists)
                        return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur" });
                }

                // Update basic information
                if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim().ToLower();

                // Update role if provided
                if (dto.Role.HasValue)
                {
                    user.Role = dto.Role.Value;
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    if (dto.Password != dto.ConfirmPassword)
                    {
                        return BadRequest(new { message = "Les mots de passe ne correspondent pas" });
                    }

                    if (dto.Password.Length < 6)
                    {
                        return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères" });
                    }

                    // Hash the new password
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Utilisateur mis à jour avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour de l'utilisateur", error = ex.Message });
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Utilisateur introuvable" });

                // Check if user has any demandes
                var hasOrders = await _context.Demandes.AnyAsync(d => d.UtilisateurId == id);
                if (hasOrders)
                {
                    return BadRequest(new { message = "Impossible de supprimer cet utilisateur car il a des demandes associées" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Utilisateur supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de l'utilisateur", error = ex.Message });
            }
        }



        [HttpPut("password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { message = "Les mots de passe ne correspondent pas" });

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mot de passe mis à jour avec succès" });
        }























    }
}*/


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Data;
using Prelevements_par_caisse.DTOs;
using Prelevements_par_caisse.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Prelevements_par_caisse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Nom = u.Nom,
                        Prenom = u.Prenom,
                        Email = u.Email,
                        Role = u.Role,
                        Is_Faveur = u.Is_Faveur
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du chargement des utilisateurs", error = ex.Message });
            }
        }

        // GET: api/Users/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            var userDto = new UserDto
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Role = user.Role,
                Is_Faveur = user.Is_Faveur
            };

            return Ok(userDto);
        }

        // PUT: api/Users/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom;
            if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/role
        [HttpPut("role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound();

            user.Role = dto.Role;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Utilisateur introuvable" });

                // Validate email uniqueness if email is being changed
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != id);

                    if (emailExists)
                        return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur" });
                }

                // Update basic information
                if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim().ToLower();

                // Update role if provided
                if (dto.Role.HasValue)
                {
                    user.Role = dto.Role.Value;
                }

                // Update Is_Faveur if provided
                if (dto.Is_Faveur.HasValue)
                {
                    user.Is_Faveur = dto.Is_Faveur.Value;
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    if (dto.Password != dto.ConfirmPassword)
                    {
                        return BadRequest(new { message = "Les mots de passe ne correspondent pas" });
                    }

                    if (dto.Password.Length < 6)
                    {
                        return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères" });
                    }

                    // Hash the new password
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Utilisateur mis à jour avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour de l'utilisateur", error = ex.Message });
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Utilisateur introuvable" });

                // Check if user has any demandes
                var hasOrders = await _context.Demandes.AnyAsync(d => d.UtilisateurId == id);
                if (hasOrders)
                {
                    return BadRequest(new { message = "Impossible de supprimer cet utilisateur car il a des demandes associées" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Utilisateur supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de l'utilisateur", error = ex.Message });
            }
        }

        [HttpPut("password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { message = "Les mots de passe ne correspondent pas" });

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mot de passe mis à jour avec succès" });
        }










        // Add this method to your existing UsersController

        [HttpGet("favored")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetFavoredUsers()
        {
            try
            {
                var favoredUsers = await _context.Users
                    .Where(u => u.Is_Faveur == true)
                    .Select(u => new
                    {
                        id = u.Id,
                        nom = u.Nom,
                        prenom = u.Prenom,
                        fullName = $"{u.Prenom} {u.Nom}",
                        email = u.Email,
                        role = u.Role
                    })
                    .OrderBy(u => u.nom)
                    .ThenBy(u => u.prenom)
                    .ToListAsync();

                return Ok(favoredUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Erreur lors du chargement des utilisateurs privilégiés",
                    error = ex.Message
                });
            }
        }











    }
}