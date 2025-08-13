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

        // GET: api/admindemandes
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
                    utilisateur = new
                    {
                        nom = d.Utilisateur.Nom,
                        prenom = d.Utilisateur.Prenom
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
                        effectuePar = d.Paiement.EffectuePar,
                        datePaiement = d.Paiement.DatePaiement
                    } : null
                })
                .ToListAsync();

            return Ok(demandes);
        }

        // GET: api/admindemandes/{id}
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
                    utilisateur = new
                    {
                        nom = d.Utilisateur.Nom,
                        prenom = d.Utilisateur.Prenom
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
                MontantTotal = montantTotal,                 // auto
                DatePaiement = DateTime.Now,                 // auto
                EffectuePar = User.Identity?.Name ?? "Admin", // auto
                ComptePaiement = dto.ComptePaiement,         // manuel
                MontantEnLettres = dto.MontantEnLettres      // manuel
            };

            _context.Paiements.Add(paiement);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Demande validée et paiement généré" });
        }



        // PUT: api/admindemandes/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var demande = await _context.Demandes
                .Include(d => d.DemandeItems)
                .Include(d => d.Paiement)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (demande == null)
                return NotFound(new { message = "Demande introuvable" });

            // Si le paiement existe déjà → on le met à jour
            if (demande.Paiement != null)
            {
                demande.Paiement.ComptePaiement = dto.ComptePaiement;
                demande.Paiement.MontantEnLettres = dto.MontantEnLettres;
                demande.Paiement.DatePaiement = DateTime.Now;
                demande.Paiement.EffectuePar = User.Identity?.Name ?? "Admin";
            }
            else
            {
                var montantTotal = demande.DemandeItems.Sum(di => di.Quantite * di.Item.PrixUnitaire);
                var paiement = new Paiement
                {
                    DemandeId = demande.Id,
                    MontantTotal = montantTotal,
                    DatePaiement = DateTime.Now,
                    EffectuePar = User.Identity?.Name ?? "Admin",
                    ComptePaiement = dto.ComptePaiement,
                    MontantEnLettres = dto.MontantEnLettres
                };
                _context.Paiements.Add(paiement);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Demande mise à jour avec succès" });
        }








        // PUT: api/admindemandes/refuser/{id}
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
