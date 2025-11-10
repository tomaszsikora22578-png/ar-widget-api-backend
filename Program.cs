using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ArWidgetApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Nazwa polityki CORS
const string ClientAppCORS = "_clientAppCORS";

// -------------------- 1. Serwisy --------------------

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientAppCORS, policy =>
    {
        policy.WithOrigins(
                    "http://127.0.0.1:5500",
                    "https://tomaszsikora22578-png.github.io",
                    "https://ar-widget-project.firebaseapp.com",
                    "https://ar-widget-project.web.app"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

// -------------------- 2. Konfiguracja Bazy Danych --------------------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Cloud Run + Cloud SQL
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // AutoDetect wykrywa MySQL lub MariaDB na podstawie connection string
    var serverVersion = ServerVersion.AutoDetect(connectionString);

    if (isCloudRun)
    {
        // Połączenie przez UNIX socket (Cloud SQL w Cloud Run)
        options.UseMySql(connectionString, serverVersion, mySqlOptions =>
        {
            mySqlOptions.UseUnixSocket($"/cloudsql/{cloudSqlInstance}");
            mySqlOptions.EnableRetryOnFailure();
        });
        Console.WriteLine($"[INFO] Użyto połączenia UNIX socket dla Cloud SQL: {cloudSqlInstance}");
    }
    else
    {
        // Lokalny MySQL
        options.UseMySql(connectionString, serverVersion);
        Console.WriteLine("[INFO] Użyto lokalnego połączenia MySQL.");
    }
});

// -------------------- 3. Inne serwisy --------------------
builder.Services.AddControllers();
builder.Services.AddAuthorization();

// -------------------- 4. Budowanie aplikacji --------------------
var app = builder.Build();

// Swagger w Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirect
app.UseHttpsRedirection();

// CORS
app.UseCors(ClientAppCORS);

// Autoryzacja
app.UseAuthorization();

// Middleware weryfikujący token klienta
app.UseMiddleware<ClientTokenMiddleware>();

// Mapowanie endpointów
app.MapControllers();

// Uruchomienie
app.Run();
