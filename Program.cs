using ArWidgetApi.Data;
using ArWidgetApi.Middleware;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Services;
using ArWidgetApi.Models;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 🔥 1) Wczytywanie Firebase Key JSON z Secret Managera
// ======================================================
string firebaseKeyJson = builder.Configuration["firebase-admin-key"];

if (string.IsNullOrEmpty(firebaseKeyJson))
{
    throw new Exception("❌ Brak klucza 'firebase-admin-key' w Secret Managerze!");
}

// Inicjalizacja Firebase Admin SDK
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromJson(firebaseKeyJson)
});

// Rejestracja serwisu FirebaseAuth
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

// ======================================================
// 🔥 2) Pozostałe serwisy
// ======================================================

const string ClientAppCORS = "_clientAppCORS";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// GCS
builder.Services.AddSingleton<GcsService>();

// ======================================================
// 🔥 3) Konfiguracja CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientAppCORS, policy =>
    {
        policy.WithOrigins(
            "http://127.0.0.1:5500",
            "https://tomaszsikora22578-png.github.io",
            "https://ar-widget-project.firebaseapp.com",
            "https://ar-widget-project.web.app",
            "https://intelicore.pl"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// ======================================================
// 🔥 4) Konfiguracja MySQL (lokalnie / Cloud Run)
// ======================================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];

var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

if (isCloudRun)
{
    connectionString = $"Server=/cloudsql/{cloudSqlInstance};Database=ArWidgetDb;Uid=ar-widget-mysql;Pwd=0S3I5ggLGtP71c]V;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure();
    });
});

// Log
Console.WriteLine(isCloudRun
    ? $"[INFO] Użyto Cloud SQL przez gniazdo UNIX: {cloudSqlInstance}"
    : "[INFO] Użyto lokalnego połączenia MySQL.");


// ======================================================
// 🔥 5) Tworzenie aplikacji
// ======================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS — musi być przed middleware
app.UseCors(ClientAppCORS);

// 🔥 Middleware klienta
app.UseMiddleware<ClientTokenMiddleware>();

// 🔥 Middleware Firebase Auth (Google Sign-In)
app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Debug endpointów
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista dostępnych endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
    Console.WriteLine(endpoint.DisplayName);
Console.WriteLine("=== Koniec listy endpointów ===");

app.Run();
