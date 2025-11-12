using ArWidgetApi.Data;
using ArWidgetApi.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1️⃣ Konfiguracja CORS
// --------------------
const string ClientAppCORS = "_clientAppCORS";

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

// --------------------
// 2️⃣ Konfiguracja bazy danych
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

if (isCloudRun)
{
    // Połączenie przez Cloud SQL Unix socket
    connectionString = $"Server=/cloudsql/{cloudSqlInstance};Database=ArWidgetDb;Uid=ar-widget-mysql;Pwd=0S3I5ggLGtP71c]V;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure();
    });

    Console.WriteLine(isCloudRun
        ? $"[INFO] Użyto Cloud SQL przez UNIX socket: {cloudSqlInstance}"
        : "[INFO] Użyto lokalnego połączenia MySQL.");
});

// --------------------
// 3️⃣ Dodanie kontrolerów i Swagger
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var app = builder.Build();

// --------------------
// 4️⃣ Middleware pipeline
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ⚡ CORS MUSI być przed middleware tokenów
app.UseCors(ClientAppCORS);

// Middleware sprawdzający tokeny
app.UseMiddleware<ClientTokenMiddleware>();

app.UseAuthorization();
app.MapControllers();

// --------------------
// 5️⃣ Logowanie endpointów przy starcie
// --------------------
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista dostępnych endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
Console.WriteLine("=== Koniec listy endpointów ===");

// --------------------
// 6️⃣ Uruchomienie aplikacji
// --------------------
app.Run();
