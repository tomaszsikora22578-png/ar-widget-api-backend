using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ArWidgetApi.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;

// Klasa ta musi znajdować się w projekcie zawierającym klasę DbContext
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Odczytanie konfiguracji (w tym ConnectionString) z pliku appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json") // Możesz dodać też appsettings.Development.json
            .Build();

        // 2. Pobranie ConnectionString
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 3. Konfiguracja DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        //  Ustawienie połączenia MySQL z Poprawną Wersją Serwera
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.Create(8, 0, 34, ServerType.MySql) // Dostosuj wersję, jeśli to konieczne
        );

        // 4. Utworzenie i zwrócenie obiektu DbContext
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}