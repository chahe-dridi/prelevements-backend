using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Models;

namespace Prelevements_par_caisse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Categorie> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Demande> Demandes { get; set; }
        public DbSet<DemandeItem> DemandeItems { get; set; }
        public DbSet<Paiement> Paiements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*  modelBuilder.Entity<Item>()
                  .Property(i => i.PrixUnitaire)
                  .HasColumnType("decimal(18,2)");

              modelBuilder.Entity<Paiement>()
                  .Property(p => p.MontantTotal)
                  .HasColumnType("decimal(18,2)");

              modelBuilder.Entity<DemandeItem>()
                  .HasOne(di => di.Demande)
                  .WithMany(d => d.DemandeItems)
                  .HasForeignKey(di => di.DemandeId)
                  .OnDelete(DeleteBehavior.Cascade);

              modelBuilder.Entity<DemandeItem>()
                  .HasOne(di => di.Item)
                  .WithMany()
                  .HasForeignKey(di => di.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);

              modelBuilder.Entity<Paiement>()
                  .HasOne(p => p.Demande)
                  .WithOne(d => d.Paiement)
                  .HasForeignKey<Paiement>(p => p.DemandeId)
                  .OnDelete(DeleteBehavior.Cascade);*/

                    modelBuilder.Entity<DemandeItem>()
                 .Property(di => di.PrixUnitaire)
                 .HasColumnType("decimal(18,2)");

                    modelBuilder.Entity<Paiement>()
                        .Property(p => p.MontantTotal)
                        .HasColumnType("decimal(18,2)");

                    modelBuilder.Entity<DemandeItem>()
                        .HasOne(di => di.Demande)
                        .WithMany(d => d.DemandeItems)
                        .HasForeignKey(di => di.DemandeId)
                        .OnDelete(DeleteBehavior.Cascade);

                    modelBuilder.Entity<DemandeItem>()
                        .HasOne(di => di.Item)
                        .WithMany()
                        .HasForeignKey(di => di.ItemId)
                        .OnDelete(DeleteBehavior.Restrict);

                    modelBuilder.Entity<Paiement>()
                        .HasOne(p => p.Demande)
                        .WithOne(d => d.Paiement)
                        .HasForeignKey<Paiement>(p => p.DemandeId)
                        .OnDelete(DeleteBehavior.Cascade);


        }





    }
}
