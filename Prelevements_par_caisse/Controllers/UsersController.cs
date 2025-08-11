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
        // Only Admin and SuperAdmin can get all users
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers()
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

        // GET: api/Users/profile
        // Any logged in user can get their profile
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
        // Update current user's profile (except role)
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

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom;
            if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/role
        // Change a user's role - Admin and SuperAdmin only
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
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Nom)) user.Nom = dto.Nom;
            if (!string.IsNullOrWhiteSpace(dto.Prenom)) user.Prenom = dto.Prenom;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;

            if (dto.Role != null) user.Role = dto.Role.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }
























    }
}
