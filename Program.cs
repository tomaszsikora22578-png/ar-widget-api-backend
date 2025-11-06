using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ArWidgetApi.Middleware;
var builder = WebApplication.CreateBuilder(args);

// ...

// 2. NOWA Konfiguracja Bazy Danych (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // options.UseInMemoryDatabase("ArWidgetDb")- USUWAMY TĘ LINIĘ

    // Używamy UseMySql
    options.UseMySql(
        connectionString,
        // Konfiguracja wersji Twojego lokalnego serwera MySQL (np. 8.0)
        ServerVersion.Create(8, 0, 34, ServerType.MySql)
    );
});
// Dodanie Serwisów do obsługi Kontrolerów API (niezbędne!)
builder.Services.AddControllers();

// Używamy nazwy, która jasno wskazuje, że polityka jest dla aplikacji klienckich
const string ClientAppCORS = "_clientAppCORS";

// Konfiguracja CORS (umożliwienie komunikacji z Frontendami)
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientAppCORS,
        policy =>
        {
             // ***** KLUCZOWA ZMIANA CORS *****
             policy.WithOrigins(
                    "http://127.0.0.1:5500", 
                    "https://tomaszsikora22578-png.github.io",
                    "https://ar-widget-project.firebaseapp.com", // Adres z błędu
                    "https://ar-widget-project.web.app"     // Typowa domena Firebase Hosting
                ) 
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Konfiguracja Swagger/OpenAPI (opcjonalne, ale bardzo przydatne)
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

// Przekierowanie HTTP na HTTPS (dla localhost)
app.UseHttpsRedirection();

// Włączenie CORS (Musi być przed UseRouting/UseEndpoints)
// Używamy nowej, poprawnej nazwy polityki
app.UseCors(ClientAppCORS);

// Middleware do weryfikacji tokena klienta (musisz to mieć!)
app.UseMiddleware<ClientTokenMiddleware>();

// Użycie autoryzacji (opcjonalne, ale dobra praktyka)
app.UseAuthorization();

// Mapowanie Kontrolerów API
app.MapControllers();


// Ostateczne uruchomienie aplikacji
app.Run();
