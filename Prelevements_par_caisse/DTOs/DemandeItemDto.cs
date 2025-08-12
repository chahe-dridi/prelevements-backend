using Microsoft.AspNetCore.Mvc;

namespace Prelevements_par_caisse.DTOs
{
    public class DemandeItemDto
    {
        public Guid ItemId { get; set; }
        public int Quantite { get; set; }
    }
}
