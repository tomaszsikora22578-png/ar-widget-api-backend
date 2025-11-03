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

        // Mozesz tu dodac konfiguracje relacji, ale na razie zostawmy domyslna
    }
}