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
        public async Task<ActionResult<IEnumerable<Demande>>> GetAllDemandes()
        {
            return await _context.Demandes
                .Include(d => d.Utilisateur)
                .Include(d => d.Categorie)
                .Include(d => d.DemandeItems)
                    .ThenInclude(di => di.Item)
                .Include(d => d.Paiement)
                .ToListAsync();
        }

        // GET: api/admindemandes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Demande>> GetDemande(Guid id)
        {
            var demande = await _context.Demandes
                .Include(d => d.Utilisateur)
                .Include(d => d.Categorie)
                .Include(d => d.DemandeItems)
                    .ThenInclude(di => di.Item)
                .Include(d => d.Paiement)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (demande == null)
                return NotFound();

            return demande;
        }

        // PUT: api/admindemandes/valider/{id}
        [HttpPut("valider/{id}")]
        public async Task<IActionResult> ValiderDemande(Guid id)
        {
            var demande = await _context.Demandes.FindAsync(id);
            if (demande == null)
                return NotFound();

            demande.Statut = StatutDemande.Validee;
            await _context.SaveChangesAsync();
            return Ok("Demande validée.");
        }

        // PUT: api/admindemandes/refuser/{id}
        [HttpPut("refuser/{id}")]
        public async Task<IActionResult> RefuserDemande(Guid id)
        {
            var demande = await _context.Demandes.FindAsync(id);
            if (demande == null)
                return NotFound();

            demande.Statut = StatutDemande.Refusee;
            await _context.SaveChangesAsync();
            return Ok("Demande refusée.");
        }

        // POST: api/admindemandes/paiement
        [HttpPost("paiement")]
        public async Task<IActionResult> EnregistrerPaiement([FromBody] PaiementDto paiementDto)
        {
            var demande = await _context.Demandes.FindAsync(paiementDto.DemandeId);
            if (demande == null || demande.Statut != StatutDemande.Validee)
                return BadRequest("Demande introuvable ou non validée.");

            // Prevent double paiement
            var existingPaiement = await _context.Paiements.FirstOrDefaultAsync(p => p.DemandeId == paiementDto.DemandeId);
            if (existingPaiement != null)
                return BadRequest("Cette demande a déjà un paiement enregistré.");

            var paiement = new Paiement
            {
                DemandeId = paiementDto.DemandeId,
                EffectuePar = paiementDto.EffectuePar,
                ComptePaiement = paiementDto.ComptePaiement,
                MontantTotal = paiementDto.MontantTotal,
                MontantEnLettres = paiementDto.MontantEnLettres
            };

            _context.Paiements.Add(paiement);
            await _context.SaveChangesAsync();
            return Ok("Paiement enregistré avec succès.");
        }
    }
}
