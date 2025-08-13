using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Data;
using Prelevements_par_caisse.DTOs;
using Prelevements_par_caisse.Models;
using System.Security.Claims;

namespace Prelevements_par_caisse.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All authenticated users
    public class DemandesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DemandesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/demandes/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Categorie>>> GetCategories()
        {
            return await _context.Categories
                .Include(c => c.Items)
                .ToListAsync();
        }

        // POST: api/demandes
        [HttpPost]
        public async Task<IActionResult> CreateDemande([FromBody] DemandeDto demandeDto)
        {
            // Get current user id from JWT "sub" claim
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (currentUserId == null)
                return Unauthorized("Utilisateur non authentifié.");

            // Validate user exists
            if (!Guid.TryParse(currentUserId, out var userGuid))
                return Unauthorized("User ID invalide.");

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
                return BadRequest("Utilisateur introuvable.");

            var category = await _context.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == demandeDto.CategorieId);
            if (category == null)
                return BadRequest("Catégorie introuvable.");

            foreach (var demandeItemDto in demandeDto.Items)
            {
                if (!category.Items.Any(i => i.Id == demandeItemDto.ItemId))
                    return BadRequest($"L'item {demandeItemDto.ItemId} n'appartient pas à cette catégorie.");
            }

            var demande = new Demande
            {
                UtilisateurId = userGuid, // Use user id from JWT only
                CategorieId = demandeDto.CategorieId,
                Statut = StatutDemande.EnAttente
            };

            _context.Demandes.Add(demande);

            foreach (var demandeItemDto in demandeDto.Items)
            {
                _context.DemandeItems.Add(new DemandeItem
                {
                    Demande = demande,
                    ItemId = demandeItemDto.ItemId,
                    Quantite = demandeItemDto.Quantite
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Demande créée avec succès", demande.Id });
        }

        // GET: api/demandes/utilisateur/{userId}
        [HttpGet("utilisateur/{userId}")]
        public async Task<ActionResult<IEnumerable<Demande>>> GetDemandesByUser(Guid userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

            if (currentUserId != userId.ToString() && currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            return await _context.Demandes
                .Include(d => d.Categorie)
                .Include(d => d.DemandeItems)
                    .ThenInclude(di => di.Item)
                .Where(d => d.UtilisateurId == userId)
                .ToListAsync();
        }


        // ========== ADMIN / SUPERADMIN ONLY ==========

        // POST: api/demandes/categories
        [HttpPost("categories")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddCategory([FromBody] CategorieDto categoryDto)
        {
            var category = new Categorie
            {
                Nom = categoryDto.Nom,
                Description = categoryDto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Catégorie ajoutée avec succès", category.Id });
        }

        // POST: api/demandes/items
        [HttpPost("items")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddItem([FromBody] ItemDto itemDto)
        {
            var category = await _context.Categories.FindAsync(itemDto.CategorieId);
            if (category == null)
                return BadRequest("Catégorie introuvable.");

            var item = new Item
            {
                Nom = itemDto.Nom,
                PrixUnitaire = itemDto.PrixUnitaire,
                CategorieId = itemDto.CategorieId
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item ajouté avec succès", item.Id });
        }
    }
}
