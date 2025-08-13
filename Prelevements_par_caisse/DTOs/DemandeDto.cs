using Microsoft.AspNetCore.Mvc;

namespace Prelevements_par_caisse.DTOs
{
    public class DemandeDto
    {
        public Guid UtilisateurId { get; set; }


        public DateTime DateDemande { get; set; }

        public Guid CategorieId { get; set; }
        public List<DemandeItemDto> Items { get; set; }
    }
}
