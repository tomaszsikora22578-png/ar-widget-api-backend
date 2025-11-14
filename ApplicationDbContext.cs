using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Models;

namespace ArWidgetApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<AnalyticsEntry> AnalyticsEntries { get; set; }
        // Dodaj DbSet dla nowej tabeli łączącej
        public DbSet<ClientProductAccess> ClientProductAccess { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // KONFIGURACJA RELACJI DLA TABELI Client_Product_Access
            modelBuilder.Entity<ClientProductAccess>()
                .HasKey(cpa => new { cpa.client_id, cpa.product_id }); // Definicja klucza złożonego

            // Definicja relacji Klient -> Dostęp
            modelBuilder.Entity<ClientProductAccess>()
                .HasOne(cpa => cpa.Client)
                .WithMany(c => c.ClientProductAccess)
                .HasForeignKey(cpa => cpa.client_id);

            // Definicja relacji Produkt -> Dostęp
            modelBuilder.Entity<ClientProductAccess>()
                .HasOne(cpa => cpa.Product)
                .WithMany(p => p.ClientProductAccess)
                .HasForeignKey(cpa => cpa.product_id);
                
            // Upewnij się, że inne konfiguracje bazy danych są dziedziczone
            base.OnModelCreating(modelBuilder);
        }
    }
}
