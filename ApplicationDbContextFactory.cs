using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ArWidgetApi.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;

// Klasa ta musi znajdowaÄ‡ siÄ™ w projekcie zawierajÄ…cym klasÄ™ DbContext
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Odczytanie konfiguracji (w tym ConnectionString) z pliku appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json") // MoĹĽesz dodaÄ‡ teĹĽ appsettings.Development.json
            .Build();

        // 2. Pobranie ConnectionString
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 3. Konfiguracja DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        //  Ustawienie poĹ‚Ä…czenia MySQL z PoprawnÄ… WersjÄ… Serwera
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.Create(8, 0, 34, ServerType.MySql) // Dostosuj wersjÄ™, jeĹ›li to konieczne
        );

        // 4. Utworzenie i zwrĂłcenie obiektu DbContext
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}