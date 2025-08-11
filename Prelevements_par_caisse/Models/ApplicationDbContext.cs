using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Models;

namespace Prelevements_par_caisse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
