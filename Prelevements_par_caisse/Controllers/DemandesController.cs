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

        [HttpGet("utilisateur/{userId}")]
        public async Task<IActionResult> GetDemandesByUser(Guid userId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized("User ID not found in token");

                if (!Guid.TryParse(currentUserId, out var currentUserGuid))
                    return Unauthorized("Invalid user ID format");

                if (currentUserGuid != userId)
                    return Unauthorized("Access denied");

                var demandes = await _context.Demandes
                    .Where(d => d.UtilisateurId == userId)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Select(d => new
                    {
                        d.Id,
                        DateDemande = d.DateDemande,
                        Statut = d.Statut.ToString(),
                        CategorieId = d.CategorieId,
                        Categorie = new
                        {
                            Id = d.Categorie.Id,
                            Nom = d.Categorie.Nom,
                            Description = d.Categorie.Description
                        },
                        DemandeItems = d.DemandeItems.Select(di => new
                        {
                            Id = di.Id,
                            Quantite = di.Quantite,
                            ItemId = di.ItemId,
                            Item = new
                            {
                                Id = di.Item.Id,
                                Nom = di.Item.Nom,
                                PrixUnitaire = di.Item.PrixUnitaire
                            }
                        }).ToList()
                    })
                    .OrderByDescending(d => d.DateDemande)
                    .ToListAsync();

                return Ok(demandes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }










        // GET: api/demandes/my-demandes (uses email from JWT)
        [HttpGet("my-demandes")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyDemandes()
        {
            try
            {
                // Get current user email from JWT
                var currentUserEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
                if (currentUserEmail == null)
                {
                    return Unauthorized("Email utilisateur non trouvé dans le token.");
                }

                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                if (user == null)
                {
                    return NotFound("Utilisateur introuvable.");
                }

                // Get demandes for this user with explicit projection to avoid circular references
                var demandes = await _context.Demandes
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Where(d => d.UtilisateurId == user.Id)
                    .Select(d => new
                    {
                        d.Id,
                        d.DateDemande,
                        d.Statut,
                        d.CategorieId,
                        Categorie = new
                        {
                            d.Categorie.Id,
                            d.Categorie.Nom,
                            d.Categorie.Description
                        },
                        DemandeItems = d.DemandeItems.Select(di => new
                        {
                            di.Id,
                            di.Quantite,
                            di.ItemId,
                            Item = new
                            {
                                di.Item.Id,
                                di.Item.Nom,
                                di.Item.PrixUnitaire
                            }
                        }).ToList()
                    })
                    .OrderByDescending(d => d.DateDemande) // Most recent first
                    .ToListAsync();

                return Ok(demandes);
            }
            catch (Exception ex)
            {
                // Log the exception details
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // PUT: api/demandes/{id} - FIXED VERSION
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] DemandeDto demandeDto)
        {
            try
            {
                // Get current user email from JWT
                var currentUserEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
                if (currentUserEmail == null)
                    return Unauthorized("Email utilisateur non trouvé dans le token.");

                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                if (user == null)
                    return NotFound("Utilisateur introuvable.");

                // Find the existing demande (only for the current user)
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .FirstOrDefaultAsync(d => d.Id == id && d.UtilisateurId == user.Id);

                if (demande == null)
                    return NotFound("Demande introuvable.");

                // Check if demande can be updated (only EnAttente status)
                if (demande.Statut != StatutDemande.EnAttente)
                    return BadRequest("Seules les demandes en attente peuvent être modifiées.");

                // Validate category exists
                var category = await _context.Categories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == demandeDto.CategorieId);
                if (category == null)
                    return BadRequest("Catégorie introuvable.");

                // Validate all items belong to the category
                foreach (var demandeItemDto in demandeDto.Items)
                {
                    if (!category.Items.Any(i => i.Id == demandeItemDto.ItemId))
                        return BadRequest($"L'item {demandeItemDto.ItemId} n'appartient pas à cette catégorie.");
                }

                // Remove existing items
                _context.DemandeItems.RemoveRange(demande.DemandeItems);

                // Update demande
                demande.CategorieId = demandeDto.CategorieId;

                // Add new items
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
                return Ok(new { Message = "Demande mise à jour avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
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