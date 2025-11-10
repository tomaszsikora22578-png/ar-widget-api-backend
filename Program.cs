using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal; // Dodano, aby użyć MySqlUnixDomainSocketFactory
using ArWidgetApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Używamy nazwy, która jasno wskazuje, że polityka jest dla aplikacji klienckich
const string ClientAppCORS = "_clientAppCORS";

// 1. Serwisy
// Konfiguracja Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// KLUCZOWA SEKCJA CORS (Nie zmieniona, ale poprawna) 
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientAppCORS,
        policy =>
        {
            policy.WithOrigins(
                        "http://127.0.0.1:5500", // Lokalny serwer dev
                        "https://tomaszsikora22578-png.github.io", // Github Pages
                        "https://ar-widget-project.firebaseapp.com", // Adres z błędu
                        "https://ar-widget-project.web.app"          // Typowa domena Firebase Hosting
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                    // .AllowCredentials(); // Dodaj, jeśli będziesz używać autoryzacji z cookies
        });
});

// 2. Konfiguracja Bazy Danych (MySQL)
// 2. Konfiguracja Bazy Danych (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Cloud Run automatycznie ustawia tę zmienną po dodaniu połączenia Cloud SQL w konsoli.
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 34));

    if (isCloudRun)
    {
        // Gniazdo UNIX (zalecane dla Cloud SQL + Cloud Run)
        var cloudSqlSocketPath = $"/cloudsql/{cloudSqlInstance}";
        var cloudSqlConnectionString =
            $"{connectionString};Server={cloudSqlSocketPath}";

        options.UseMySql(
            cloudSqlConnectionString,
            serverVersion,
            mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure();
            });

        Console.WriteLine($"[INFO] Użyto Cloud SQL socket: {cloudSqlSocketPath}");
    }
    else
    {
        // Połączenie lokalne (np. przy uruchamianiu z Visual Studio)
        options.UseMySql(connectionString, serverVersion);
        Console.WriteLine("[INFO] Użyto lokalnego połączenia MySQL.");
    }
});

// KONIEC NOWEJ LOGIKI DLA CLOUD SQL W CLOUD RUN 

// Dodanie Serwisów do obsługi Kontrolerów API
builder.Services.AddControllers();

// Użycie autoryzacji (dodanie serwisu)
builder.Services.AddAuthorization();

// --- 3. BUDOWANIE APLIKACJI I KONFIGURACJA POTOKU ---

var app = builder.Build();

// Użyj tego, aby zobaczyć błędy podczas uruchamiania (tylko w Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Przekierowanie HTTP na HTTPS (dobra praktyka)
app.UseHttpsRedirection();

// WŁĄCZENIE CORS (Musi być przed UseAuthorization i UseEndpoints) 
app.UseCors(ClientAppCORS);

// Zmiana kolejności: Najpierw UseAuthorization, potem Middleware (Ważne dla niektórych scenariuszy) 

// 1. Użycie autoryzacji (standardowe middleware)
app.UseAuthorization();

// 2. Middleware do weryfikacji tokena klienta (ClientTokenMiddleware)
// To jest niestandardowy middleware i powinno być użyte po standardowym Użyciu Autoryzacji
app.UseMiddleware<ClientTokenMiddleware>();


// Mapowanie Kontrolerów API (endpoints)
app.MapControllers();


// Ostateczne uruchomienie aplikacji
app.Run();
