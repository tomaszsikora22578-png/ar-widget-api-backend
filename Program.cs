using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Middleware;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Nazwa polityki CORS
const string ClientAppCORS = "_clientAppCORS";

// 1. Serwisy
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// 2. Konfiguracja bazy danych
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
        // Retry w razie chwilowych problemów z połączeniem
        mySqlOptions.EnableRetryOnFailure();
    });

    Console.WriteLine(isCloudRun
        ? $"[INFO] Użyto Cloud SQL przez UNIX socket: {cloudSqlInstance}"
        : "[INFO] Użyto lokalnego połączenia MySQL.");
});

// Dodanie kontrolerów
builder.Services.AddControllers();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(ClientAppCORS);
app.UseAuthorization();
app.UseMiddleware<ClientTokenMiddleware>();
app.MapControllers();


// logowanie endpoitn:
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista dostępnych endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
Console.WriteLine("=== Koniec listy endpointów ===");

app.Run();
