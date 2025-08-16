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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDemande(Guid id)
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

        [HttpPut("valider/{id}")]
        public async Task<IActionResult> ValiderDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        // Updated method to handle status changes
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        [HttpPut("refuser/{id}")]
        public async Task<IActionResult> RefuserDemande(Guid id)
        {
            var demande = await _context.Demandes.FindAsync(id);
            if (demande == null)
                return NotFound(new { message = "Demande introuvable" });

            demande.Statut = StatutDemande.Refusee;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Demande refusée" });
        }
    }
}