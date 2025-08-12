using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Data;
using Prelevements_par_caisse.DTOs;
using Prelevements_par_caisse.Models;

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
            // Validate user exists and matches the logged in user or has admin role
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (currentUserId == null || currentUserId != demandeDto.UtilisateurId.ToString())
            {
                // Optionally check if user has Admin role - but usually better to only allow self
                return Unauthorized("Vous ne pouvez créer une demande que pour vous-même.");
            }

            var user = await _context.Users.FindAsync(demandeDto.UtilisateurId);
            if (user == null)
                return BadRequest("Utilisateur introuvable.");

            var category = await _context.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == demandeDto.CategorieId);
            if (category == null)
                return BadRequest("Catégorie introuvable.");

            // Validate selected items belong to this category
            foreach (var itemDto in demandeDto.Items)
            {
                if (!category.Items.Any(i => i.Id == itemDto.ItemId))
                    return BadRequest($"L'item {itemDto.ItemId} n'appartient pas à cette catégorie.");
            }

            var demande = new Demande
            {
                UtilisateurId = demandeDto.UtilisateurId,
                CategorieId = demandeDto.CategorieId,
                Statut = StatutDemande.EnAttente
            };

            _context.Demandes.Add(demande);

            foreach (var itemDto in demandeDto.Items)
            {
                _context.DemandeItems.Add(new DemandeItem
                {
                    Demande = demande,
                    ItemId = itemDto.ItemId,
                    Quantite = itemDto.Quantite
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Demande créée avec succès", demande.Id });
        }

        // GET: api/demandes/utilisateur/{userId}
        [HttpGet("utilisateur/{userId}")]
        public async Task<ActionResult<IEnumerable<Demande>>> GetDemandesByUser(Guid userId)
        {
            // Same user can only access their demandes or admin can access anyone
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

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
