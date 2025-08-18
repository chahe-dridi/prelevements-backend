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
    [Authorize]
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
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Items)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // POST: api/demandes
        [HttpPost]
        public async Task<IActionResult> CreateDemande([FromBody] DemandeDto demandeDto)
        {
            try
            {
                // Get current user id from JWT claims - try both claim types
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("Utilisateur non authentifié - ID utilisateur introuvable dans le token.");
                }

                // Validate user ID format
                if (!Guid.TryParse(currentUserId, out var userGuid))
                {
                    return Unauthorized("Format d'ID utilisateur invalide.");
                }

                // Validate user exists
                var user = await _context.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return BadRequest("Utilisateur introuvable dans la base de données.");
                }

                // Validate input data
                if (demandeDto == null)
                {
                    return BadRequest("Données de demande manquantes.");
                }

                if (demandeDto.Items == null || !demandeDto.Items.Any())
                {
                    return BadRequest("Au moins un item doit être sélectionné.");
                }

                // Validate category exists
                var category = await _context.Categories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == demandeDto.CategorieId);
                if (category == null)
                {
                    return BadRequest("Catégorie introuvable.");
                }

                // Validate all items belong to the category and have positive quantities
                foreach (var demandeItemDto in demandeDto.Items)
                {
                    if (!category.Items.Any(i => i.Id == demandeItemDto.ItemId))
                    {
                        return BadRequest($"L'item {demandeItemDto.ItemId} n'appartient pas à cette catégorie.");
                    }

                    if (demandeItemDto.Quantite <= 0)
                    {
                        return BadRequest($"La quantité pour l'item {demandeItemDto.ItemId} doit être positive.");
                    }
                }

                // Create demande
                var demande = new Demande
                {
                    UtilisateurId = userGuid,
                    CategorieId = demandeDto.CategorieId,
                    Statut = StatutDemande.EnAttente,
                    DateDemande = DateTime.UtcNow
                };

                _context.Demandes.Add(demande);
                await _context.SaveChangesAsync(); // Save demande first to get the ID

                // Add demande items
                foreach (var demandeItemDto in demandeDto.Items)
                {
                    var demandeItem = new DemandeItem
                    {
                        DemandeId = demande.Id,
                        ItemId = demandeItemDto.ItemId,
                        Quantite = demandeItemDto.Quantite
                    };

                    _context.DemandeItems.Add(demandeItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Demande créée avec succès",
                    Id = demande.Id,
                    DateDemande = demande.DateDemande,
                    Statut = demande.Statut.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur lors de la création de la demande: {ex.Message}");
            }
        }

        // GET: api/demandes/utilisateur/{userId}
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

        // GET: api/demandes/my-demandes
        [HttpGet("my-demandes")]
        public async Task<ActionResult<object>> GetMyDemandes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get current user id from JWT claims - try both claim types
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("Utilisateur non authentifié.");
                }

                if (!Guid.TryParse(currentUserId, out var userGuid))
                {
                    return Unauthorized("Format d'ID utilisateur invalide.");
                }

                // Validate user exists
                var user = await _context.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return NotFound("Utilisateur introuvable.");
                }

                // Calculate pagination
                var skip = (page - 1) * pageSize;

                // Get total count
                var totalCount = await _context.Demandes
                    .Where(d => d.UtilisateurId == userGuid)
                    .CountAsync();

                // Get paginated demandes
                var demandes = await _context.Demandes
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Where(d => d.UtilisateurId == userGuid)
                    .OrderByDescending(d => d.DateDemande)
                    .Skip(skip)
                    .Take(pageSize)
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
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var result = new
                {
                    Data = demandes,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = page > 1,
                        HasNext = page < totalPages
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // PUT: api/demandes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] DemandeDto demandeDto)
        {
            try
            {
                // Get current user id from JWT claims - consistent with CreateDemande
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized("Utilisateur non authentifié.");

                if (!Guid.TryParse(currentUserId, out var userGuid))
                    return Unauthorized("Format d'ID utilisateur invalide.");

                // Find user
                var user = await _context.Users.FindAsync(userGuid);
                if (user == null)
                    return NotFound("Utilisateur introuvable.");

                // Find the existing demande (only for the current user)
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .FirstOrDefaultAsync(d => d.Id == id && d.UtilisateurId == userGuid);

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

        // DELETE: api/demandes/{id}/cancel
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> CancelDemande(Guid id)
        {
            try
            {
                // Get current user id from JWT claims - consistent with other methods
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized("Utilisateur non authentifié.");

                if (!Guid.TryParse(currentUserId, out var userGuid))
                    return Unauthorized("Format d'ID utilisateur invalide.");

                // Find user
                var user = await _context.Users.FindAsync(userGuid);
                if (user == null)
                    return NotFound("Utilisateur introuvable.");

                // Find the existing demande (only for the current user)
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .FirstOrDefaultAsync(d => d.Id == id && d.UtilisateurId == userGuid);

                if (demande == null)
                    return NotFound("Demande introuvable.");

                // Check if demande can be cancelled (only EnAttente status)
                if (demande.Statut != StatutDemande.EnAttente)
                    return BadRequest("Seules les demandes en attente peuvent être annulées.");

                // Remove demande and its items (cascade delete)
                _context.DemandeItems.RemoveRange(demande.DemandeItems);
                _context.Demandes.Remove(demande);

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Demande annulée avec succès" });
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
            try
            {
                if (string.IsNullOrWhiteSpace(categoryDto.Nom))
                    return BadRequest("Le nom de la catégorie est requis.");

                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Nom.ToLower() == categoryDto.Nom.ToLower().Trim());
                if (existingCategory != null)
                    return BadRequest("Une catégorie avec ce nom existe déjà.");

                var category = new Categorie
                {
                    Nom = categoryDto.Nom.Trim(),
                    Description = categoryDto.Description?.Trim()
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Catégorie ajoutée avec succès", Id = category.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // GET: api/demandes/categories/{id}
        [HttpGet("categories/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound("Catégorie introuvable.");

                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // PUT: api/demandes/categories/{id}
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategorieDto categoryDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryDto.Nom))
                    return BadRequest("Le nom de la catégorie est requis.");

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound("Catégorie introuvable.");

                // Check if another category with the same name exists (excluding current one)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id != id && c.Nom.ToLower() == categoryDto.Nom.ToLower().Trim());
                if (existingCategory != null)
                    return BadRequest("Une autre catégorie avec ce nom existe déjà.");

                category.Nom = categoryDto.Nom.Trim();
                category.Description = categoryDto.Description?.Trim();

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Catégorie mise à jour avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // DELETE: api/demandes/categories/{id}
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound("Catégorie introuvable.");

                // Check if category has associated demandes using separate query
                var hasAssociatedDemandes = await _context.Demandes
                    .AnyAsync(d => d.CategorieId == id);

                if (hasAssociatedDemandes)
                    return BadRequest("Impossible de supprimer cette catégorie car elle contient des demandes associées.");

                // Delete all items in this category first
                if (category.Items != null && category.Items.Any())
                {
                    _context.Items.RemoveRange(category.Items);
                }

                // Delete the category
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Catégorie supprimée avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // POST: api/demandes/items
        [HttpPost("items")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddItem([FromBody] ItemDto itemDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemDto.Nom))
                    return BadRequest("Le nom de l'item est requis.");

                if (itemDto.PrixUnitaire <= 0)
                    return BadRequest("Le prix unitaire doit être positif.");

                var category = await _context.Categories.FindAsync(itemDto.CategorieId);
                if (category == null)
                    return BadRequest("Catégorie introuvable.");

                // Check if item name already exists in this category
                var existingItem = await _context.Items
                    .FirstOrDefaultAsync(i => i.CategorieId == itemDto.CategorieId &&
                                            i.Nom.ToLower() == itemDto.Nom.ToLower().Trim());
                if (existingItem != null)
                    return BadRequest("Un item avec ce nom existe déjà dans cette catégorie.");

                var item = new Item
                {
                    Nom = itemDto.Nom.Trim(),
                    PrixUnitaire = itemDto.PrixUnitaire,
                    CategorieId = itemDto.CategorieId
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Item ajouté avec succès", Id = item.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // GET: api/demandes/items/{id}
        [HttpGet("items/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            try
            {
                var item = await _context.Items
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null)
                    return NotFound("Item introuvable.");

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // PUT: api/demandes/items/{id}
        [HttpPut("items/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] ItemDto itemDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemDto.Nom))
                    return BadRequest("Le nom de l'item est requis.");

                if (itemDto.PrixUnitaire <= 0)
                    return BadRequest("Le prix unitaire doit être positif.");

                var item = await _context.Items.FindAsync(id);
                if (item == null)
                    return NotFound("Item introuvable.");

                var category = await _context.Categories.FindAsync(itemDto.CategorieId);
                if (category == null)
                    return BadRequest("Catégorie introuvable.");

                // Check if another item with the same name exists in the target category (excluding current one)
                var existingItem = await _context.Items
                    .FirstOrDefaultAsync(i => i.Id != id &&
                                            i.CategorieId == itemDto.CategorieId &&
                                            i.Nom.ToLower() == itemDto.Nom.ToLower().Trim());
                if (existingItem != null)
                    return BadRequest("Un autre item avec ce nom existe déjà dans cette catégorie.");

                item.Nom = itemDto.Nom.Trim();
                item.PrixUnitaire = itemDto.PrixUnitaire;
                item.CategorieId = itemDto.CategorieId;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Item mis à jour avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // DELETE: api/demandes/items/{id}
        [HttpDelete("items/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            try
            {
                var item = await _context.Items.FindAsync(id);
                if (item == null)
                    return NotFound("Item introuvable.");

                // Check if item has associated demande items using separate query
                var hasDemandeItems = await _context.DemandeItems
                    .AnyAsync(di => di.ItemId == id);

                if (hasDemandeItems)
                    return BadRequest("Impossible de supprimer cet item car il est utilisé dans des demandes existantes.");

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Item supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }
    }
}