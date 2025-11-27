using ArWidgetApi.Data;
using ArWidgetApi.Middleware;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Nazwa polityki CORS
const string ClientAppCORS = "_clientAppCORS";

// 🔹 Rejestracja serwisów
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Konfiguracja CORS — poprawne domeny frontendu
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
        // ✅ Poprawka: AllowAnyHeader jest niezbędne dla X-Client-Token
        .AllowAnyHeader()
        // ✅ Poprawka: Prawidłowe zezwolenie na wszystkie metody (GET, POST, OPTIONS)
        .AllowAnyMethod();
        // Usunięto .AllowCredentials(), ponieważ nie było potrzebne i komplikowało CORS
    });
});

// 🔹 Konfiguracja połączenia z bazą
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
// Rejestracja serwisów
builder.Services.AddSingleton<GcsService>(); 
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

if (isCloudRun)
{
    // Użycie połączenia przez Gniazdo UNIX, wymagające konfiguracji w Cloud Run Connections
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

// 🔹 Logowanie do konsoli
Console.WriteLine(isCloudRun
    ? $"[INFO] Użyto Cloud SQL przez gniazdo UNIX: {cloudSqlInstance}"
    : "[INFO] Użyto lokalnego połączenia MySQL.");

// 🔹 Tworzymy aplikację
var app = builder.Build();

// 🔹 Swagger tylko lokalnie
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 Middleware kolejność — to BARDZO ważne
app.UseHttpsRedirection();

// ✅ CORS musi być PRZED middleware tokenowym
app.UseCors(ClientAppCORS);

// 🔹 Middleware autoryzacji tokenem klienta
app.UseMiddleware<ClientTokenMiddleware>();

// 🔹 Autoryzacja / kontrolery
app.UseAuthorization();
app.MapControllers();

// 🔹 Debug: logowanie endpointów
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista dostępnych endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
Console.WriteLine("=== Koniec listy endpointów ===");

app.Run();
