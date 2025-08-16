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
                            quantite = di.Quantite
                        }),
                        demandeItems = d.DemandeItems.Select(di => new {
                            id = di.Id,
                            quantite = di.Quantite,
                            item = new
                            {
                                nom = di.Item.Nom,
                                prixUnitaire = di.Item.PrixUnitaire
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
                            item = new
                            {
                                nom = di.Item.Nom,
                                prixUnitaire = di.Item.PrixUnitaire
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

                // Changer le statut
                demande.Statut = StatutDemande.Validee;

                // Calcul du montant total
                var montantTotal = demande.DemandeItems.Sum(di => di.Quantite * di.Item.PrixUnitaire);

                // Création du paiement
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

        // Updated method to handle status changes
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

                // Calculate total amount
                var montantTotal = demande.DemandeItems.Sum(di => di.Quantite * di.Item.PrixUnitaire);

                // Si le paiement existe déjà → on le met à jour
                if (demande.Paiement != null)
                {
                    demande.Paiement.ComptePaiement = dto.ComptePaiement;
                    demande.Paiement.MontantEnLettres = dto.MontantEnLettres;
                    demande.Paiement.MontantTotal = montantTotal; // Update total amount
                    demande.Paiement.DatePaiement = DateTime.Now;
                    demande.Paiement.EffectuePar = dto.EffectuePar;
                }
                else
                {
                    // Create new payment if doesn't exist
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

        // NEW: Delete demande endpoint
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

                // Remove related payment if exists
                if (demande.Paiement != null)
                {
                    _context.Paiements.Remove(demande.Paiement);
                }

                // Remove related demande items
                if (demande.DemandeItems != null && demande.DemandeItems.Any())
                {
                    _context.DemandeItems.RemoveRange(demande.DemandeItems);
                }

                // Remove the demande itself
                _context.Demandes.Remove(demande);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande supprimée avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur lors de la suppression: {ex.Message}" });
            }
        }

        // NEW: Bulk delete endpoint for multiple demandes
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
                    // Remove related payment if exists
                    if (demande.Paiement != null)
                    {
                        _context.Paiements.Remove(demande.Paiement);
                    }

                    // Remove related demande items
                    if (demande.DemandeItems != null && demande.DemandeItems.Any())
                    {
                        _context.DemandeItems.RemoveRange(demande.DemandeItems);
                    }

                    // Remove the demande itself
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
    }
}