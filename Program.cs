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
    // options.UseInMemoryDatabase("ArWidgetDb")- USUWAMY TÄ LINIÄ

    // UĹĽywamy UseMySql
    options.UseMySql(
        connectionString,
        // Konfiguracja wersji Twojego lokalnego serwera MySQL (np. 8.0)
        ServerVersion.Create(8, 0, 34, ServerType.MySql)
    );
});
// Dodanie SerwisĂłw do obsĹ‚ugi KontrolerĂłw API (niezbÄ™dne!)
builder.Services.AddControllers();

// Konfiguracja CORS (umoĹĽliwienie komunikacji z GitHub Pages)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5500", "https://tomaszsikora22578-png.github.io") // Dodaj adres Twojego dema!
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Konfiguracja Swagger/OpenAPI (opcjonalne, ale bardzo przydatne)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. BUDOWANIE APLIKACJI I KONFIGURACJA POTOKU ---

var app = builder.Build();

// UĹĽyj tego, aby zobaczyÄ‡ bĹ‚Ä™dy podczas uruchamiania (tylko w Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Przekierowanie HTTP na HTTPS (dla localhost)
app.UseHttpsRedirection();

// WĹ‚Ä…czenie CORS (Musi byÄ‡ przed UseRouting/UseEndpoints)
app.UseCors("AllowSpecificOrigin");

// Middleware do weryfikacji tokena klienta (musisz to mieÄ‡!)
app.UseMiddleware<ClientTokenMiddleware>();

// UĹĽycie autoryzacji (opcjonalne, ale dobra praktyka)
app.UseAuthorization();

// Mapowanie KontrolerĂłw API
app.MapControllers();


//  Wstrzymanie (do debugowania) zostaĹ‚o usuniÄ™te.
// JeĹ›li chcesz, aby konsola nie zamykaĹ‚a siÄ™ od razu:
// Console.WriteLine("NaciĹ›nij Enter, aby zamknÄ…Ä‡...");
// Console.ReadLine();


// Ostateczne uruchomienie aplikacji
app.Run();
// Klasa do inicjalizacji danych testowych
