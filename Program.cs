using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ArWidgetApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ... reszta kodu, która została pominięta dla zwięzłości (np. konfiguracja Logowania, itp.)

// 2. Konfiguracja Bazy Danych (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Używamy UseMySql
    options.UseMySql(
        connectionString,
        // Konfiguracja wersji Twojego serwera MySQL
        ServerVersion.Create(8, 0, 34, ServerType.MySql)
    );
});

// Dodanie Serwisów do obsługi Kontrolerów API
builder.Services.AddControllers();

// Używamy nazwy, która jasno wskazuje, że polityka jest dla aplikacji klienckich
// Zmieniamy na readonly string (lub pozostawiamy const)
const string ClientAppCORS = "_clientAppCORS";

// 🌟🌟🌟 KLUCZOWA SEKCJA CORS 🌟🌟🌟
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientAppCORS,
        policy =>
        {
            policy.WithOrigins(
                        "http://127.0.0.1:5500", // Lokalny serwer dev
                        "https://tomaszsikora22578-png.github.io", // Github Pages
                        "https://ar-widget-project.firebaseapp.com", // Adres z błędu
                        "https://ar-widget-project.web.app"       // Typowa domena Firebase Hosting
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                    // Jeśli używasz cookies/sesji lub autoryzacji bazującej na tokenach, które są przesyłane jako credential, dodaj .AllowCredentials()
        });
});

// Konfiguracja Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. BUDOWANIE APLIKACJI I KONFIGURACJA POTOKU ---

var app = builder.Build();

// Użyj tego, aby zobaczyć błędy podczas uruchamiania (tylko w Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Przekierowanie HTTP na HTTPS (dobra praktyka)
app.UseHttpsRedirection();

//  WŁĄCZENIE CORS (Musi być przed UseRouting/UseEndpoints) 
app.UseCors(ClientAppCORS);

// Middleware do weryfikacji tokena klienta (ClientTokenMiddleware)
app.UseMiddleware<ClientTokenMiddleware>();

// Użycie autoryzacji (jeśli jest potrzebna)
app.UseAuthorization();

// Mapowanie Kontrolerów API (endpoints)
app.MapControllers();


// Ostateczne uruchomienie aplikacji
app.Run();
