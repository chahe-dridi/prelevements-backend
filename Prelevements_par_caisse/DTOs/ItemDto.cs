using Microsoft.AspNetCore.Mvc;

namespace Prelevements_par_caisse.DTOs
{
    public class ItemDto
    {
        public string Nom { get; set; }
        public decimal PrixUnitaire { get; set; }
        public Guid CategorieId { get; set; }
    }



}
