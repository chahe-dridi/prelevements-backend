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
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminDemandesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminDemandesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDemandes()
        {
            try
            {
                var demandes = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .Select(d => new
                    {
                        id = d.Id,
                        statut = d.Statut.ToString(),
                        dateDemande = d.DateDemande,
                        utilisateur = new
                        {
                            nom = d.Utilisateur.Nom,
                            prenom = d.Utilisateur.Prenom,
                            email = d.Utilisateur.Email
                        },
                        categorie = new
                        {
                            nom = d.Categorie.Nom
                        },
                        items = d.DemandeItems.Select(di => new {
                            id = di.Id,
                            nom = di.Item.Nom,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire
                        }),
                        demandeItems = d.DemandeItems.Select(di => new {
                            id = di.Id,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire,
                            description = di.Description,
                            item = new
                            {
                                id = di.Item.Id,
                                nom = di.Item.Nom
                            }
                        }),
                        paiement = d.Paiement != null ? new
                        {
                            montantTotal = d.Paiement.MontantTotal,
                            comptePaiement = d.Paiement.ComptePaiement,
                            montantEnLettres = d.Paiement.MontantEnLettres,
                            effectuePar = d.Paiement.EffectuePar,
                            datePaiement = d.Paiement.DatePaiement
                        } : null
                    })
                    .ToListAsync();

                return Ok(demandes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .Select(d => new
                    {
                        id = d.Id,
                        statut = d.Statut.ToString(),
                        dateDemande = d.DateDemande,
                        utilisateur = new
                        {
                            nom = d.Utilisateur.Nom,
                            prenom = d.Utilisateur.Prenom,
                            email = d.Utilisateur.Email,
                        },
                        categorie = new
                        {
                            nom = d.Categorie.Nom
                        },
                        demandeItems = d.DemandeItems.Select(di => new {
                            id = di.Id,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire,
                            description = di.Description,
                            item = new
                            {
                                id = di.Item.Id,
                                nom = di.Item.Nom
                            }
                        }),
                        paiement = d.Paiement != null ? new
                        {
                            montantTotal = d.Paiement.MontantTotal,
                            comptePaiement = d.Paiement.ComptePaiement,
                            montantEnLettres = d.Paiement.MontantEnLettres,
                            effectuePar = d.Paiement.EffectuePar,
                            datePaiement = d.Paiement.DatePaiement
                        } : null
                    })
                    .FirstOrDefaultAsync(d => d.id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                return Ok(demande);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("valider/{id}")]
        public async Task<IActionResult> ValiderDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                // Update prices for each demande item
                if (dto.DemandeItems != null && dto.DemandeItems.Any())
                {
                    foreach (var dtoItem in dto.DemandeItems)
                    {
                        var demandeItem = demande.DemandeItems.FirstOrDefault(di => di.Id == dtoItem.Id);
                        if (demandeItem != null && dtoItem.PrixUnitaire.HasValue)
                        {
                            demandeItem.PrixUnitaire = dtoItem.PrixUnitaire.Value;
                        }
                        if (!string.IsNullOrEmpty(dtoItem.Description))
                            {
                                demandeItem.Description = dtoItem.Description;
                            }
                    }
                }

                // Change status
                demande.Statut = StatutDemande.Validee;

                // Calculate total amount using the prices set by admin
                var montantTotal = demande.DemandeItems.Sum(di =>
                    di.Quantite * (di.PrixUnitaire ?? 0));

                // Create payment
                var paiement = new Paiement
                {
                    DemandeId = demande.Id,
                    MontantTotal = montantTotal,
                    DatePaiement = DateTime.Now,
                    EffectuePar = dto.EffectuePar,
                    ComptePaiement = dto.ComptePaiement,
                    MontantEnLettres = dto.MontantEnLettres
                };

                _context.Paiements.Add(paiement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande validée et paiement généré" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                // Update prices for each demande item if provided
                if (dto.DemandeItems != null && dto.DemandeItems.Any())
                {
                    foreach (var dtoItem in dto.DemandeItems)
                    {
                        var demandeItem = demande.DemandeItems.FirstOrDefault(di => di.Id == dtoItem.Id);
                        if (demandeItem != null && dtoItem.PrixUnitaire.HasValue)
                        {
                            demandeItem.PrixUnitaire = dtoItem.PrixUnitaire.Value;
                        }
                        demandeItem.Description = dtoItem.Description;
                    }
                }

                // Update status if provided
                if (!string.IsNullOrEmpty(dto.Statut))
                {
                    if (Enum.TryParse<StatutDemande>(dto.Statut, true, out var newStatus))
                    {
                        demande.Statut = newStatus;
                    }
                    else
                    {
                        return BadRequest(new { message = "Statut invalide" });
                    }
                }

                // Calculate total amount using the current prices
                var montantTotal = demande.DemandeItems.Sum(di =>
                    di.Quantite * (di.PrixUnitaire ?? 0));

                // Update or create payment
                if (demande.Paiement != null)
                {
                    demande.Paiement.ComptePaiement = dto.ComptePaiement;
                    demande.Paiement.MontantEnLettres = dto.MontantEnLettres;
                    demande.Paiement.MontantTotal = montantTotal;
                    demande.Paiement.DatePaiement = DateTime.Now;
                    demande.Paiement.EffectuePar = dto.EffectuePar;
                }
                else
                {
                    var paiement = new Paiement
                    {
                        DemandeId = demande.Id,
                        MontantTotal = montantTotal,
                        DatePaiement = DateTime.Now,
                        EffectuePar = dto.EffectuePar,
                        ComptePaiement = dto.ComptePaiement,
                        MontantEnLettres = dto.MontantEnLettres
                    };
                    _context.Paiements.Add(paiement);
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Demande mise à jour avec succès",
                    newStatus = demande.Statut.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("refuser/{id}")]
        public async Task<IActionResult> RefuserDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes.FindAsync(id);
                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                demande.Statut = StatutDemande.Refusee;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Demande refusée" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                if (demande.Paiement != null)
                {
                    _context.Paiements.Remove(demande.Paiement);
                }

                if (demande.DemandeItems != null && demande.DemandeItems.Any())
                {
                    _context.DemandeItems.RemoveRange(demande.DemandeItems);
                }

                _context.Demandes.Remove(demande);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande supprimée avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur lors de la suppression: {ex.Message}" });
            }
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultipleDemandes([FromBody] List<Guid> demandeIds)
        {
            try
            {
                if (demandeIds == null || !demandeIds.Any())
                    return BadRequest(new { message = "Aucune demande sélectionnée" });

                var demandes = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .Include(d => d.Paiement)
                    .Where(d => demandeIds.Contains(d.Id))
                    .ToListAsync();

                if (!demandes.Any())
                    return NotFound(new { message = "Aucune demande trouvée avec les IDs fournis" });

                var deletedCount = 0;

                foreach (var demande in demandes)
                {
                    if (demande.Paiement != null)
                    {
                        _context.Paiements.Remove(demande.Paiement);
                    }

                    if (demande.DemandeItems != null && demande.DemandeItems.Any())
                    {
                        _context.DemandeItems.RemoveRange(demande.DemandeItems);
                    }

                    _context.Demandes.Remove(demande);
                    deletedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{deletedCount} demande(s) supprimée(s) avec succès",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur lors de la suppression multiple: {ex.Message}" });
            }
        }
















        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var totalDemandes = await _context.Demandes.CountAsync();
                var totalSpent = await _context.Paiements.SumAsync(p => p.MontantTotal);
                var totalUsers = await _context.Users.CountAsync();

                var demandesParStatut = await _context.Demandes
                    .GroupBy(d => d.Statut)
                    .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Statut, x => x.Count);

                var topUsers = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Paiement)
                    .GroupBy(d => d.UtilisateurId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Utilisateur.Nom,
                        Prenom = g.First().Utilisateur.Prenom,
                        Email = g.First().Utilisateur.Email,
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                var topCategories = await _context.Demandes
                    .Include(d => d.Categorie)
                    .Include(d => d.Paiement)
                    .GroupBy(d => d.CategorieId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Categorie.Nom,
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                var recentDemandes = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.Paiement)
                    .OrderByDescending(d => d.DateDemande)
                    .Take(10)
                    .Select(d => new
                    {
                        Id = d.Id,
                        DateDemande = d.DateDemande,
                        Statut = d.Statut.ToString(),
                        Utilisateur = new
                        {
                            Nom = d.Utilisateur.Nom,
                            Prenom = d.Utilisateur.Prenom
                        },
                        Categorie = new
                        {
                            Nom = d.Categorie.Nom
                        },
                        MontantTotal = d.Paiement != null ? d.Paiement.MontantTotal : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalDemandes = totalDemandes,
                    TotalSpent = totalSpent,
                    TotalUsers = totalUsers,
                    DemandesParStatut = demandesParStatut,
                    TopUsers = topUsers,
                    TopCategories = topCategories,
                    RecentDemandes = recentDemandes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpGet("analytics/filtered")]
        public async Task<IActionResult> GetFilteredAnalytics(
            [FromQuery] DateTime? dateDebut,
            [FromQuery] DateTime? dateFin,
            [FromQuery] Guid? categorieId,
            [FromQuery] Guid? itemId,
            [FromQuery] Guid? utilisateurId,
            [FromQuery] string statut)
        {
            try
            {
                var query = _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .AsQueryable();

                if (dateDebut.HasValue)
                    query = query.Where(d => d.DateDemande >= dateDebut.Value);

                if (dateFin.HasValue)
                    query = query.Where(d => d.DateDemande <= dateFin.Value.AddDays(1));

                if (categorieId.HasValue)
                    query = query.Where(d => d.CategorieId == categorieId.Value);

                if (itemId.HasValue)
                    query = query.Where(d => d.DemandeItems.Any(di => di.ItemId == itemId.Value));

                if (utilisateurId.HasValue)
                    query = query.Where(d => d.UtilisateurId == utilisateurId.Value);

                if (!string.IsNullOrEmpty(statut) && Enum.TryParse<StatutDemande>(statut, out var statutEnum))
                    query = query.Where(d => d.Statut == statutEnum);

                var demandes = await query.ToListAsync();

                var totalDemandes = demandes.Count;
                var totalSpent = demandes.Sum(d => d.Paiement?.MontantTotal ?? 0);
                var totalUsers = demandes.Select(d => d.UtilisateurId).Distinct().Count();
                var averagePerDemande = totalDemandes > 0 ? totalSpent / totalDemandes : 0;

                var demandesParStatut = demandes
                    .GroupBy(d => d.Statut)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var topUsers = demandes
                    .GroupBy(d => d.UtilisateurId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Utilisateur.Nom,
                        Prenom = g.First().Utilisateur.Prenom,
                        Email = g.First().Utilisateur.Email,
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement?.MontantTotal ?? 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToList();

                var topCategories = demandes
                    .GroupBy(d => d.CategorieId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Categorie.Nom,
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement?.MontantTotal ?? 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToList();

                return Ok(new
                {
                    TotalDemandes = totalDemandes,
                    TotalSpent = totalSpent,
                    TotalUsers = totalUsers,
                    AveragePerDemande = averagePerDemande,
                    DemandesParStatut = demandesParStatut,
                    TopUsers = topUsers,
                    TopCategories = topCategories
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        Id = u.Id,
                        Nom = u.Nom,
                        Prenom = u.Prenom,
                        Email = u.Email
                    })
                    .OrderBy(u => u.Nom)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }




    }
}