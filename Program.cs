using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal; // Dla MySqlUnixDomainSocketFactory
using ArWidgetApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Nazwa polityki CORS
const string ClientAppCORS = "_clientAppCORS";

// 1. Serwisy
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. CORS
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

// 3. Baza danych (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Cloud Run automatycznie ustawia tę zmienną środowiskową, jeśli dodałeś połączenie do Cloud SQL
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"]; 
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

// Użycie Pomelo + MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var serverVersion = ServerVersion.Create(8, 0, 34, ServerType.MySql);

    if (isCloudRun)
    {
        // Połączenie przez UNIX socket (Cloud SQL)
        options.UseMySql(connectionString, serverVersion, mysqlOptions =>
        {
            mysqlOptions
                .UseMySqlOptions(conn => conn
                    .SocketFactory(typeof(MySqlUnixDomainSocketFactory))
                );
        });

        Console.WriteLine($"[INFO] Użyto połączenia Cloud SQL przez UNIX socket: {cloudSqlInstance}");
    }
    else
    {
        // Standardowe połączenie TCP (lokalne środowisko dev)
        options.UseMySql(connectionString, serverVersion);
        Console.WriteLine("[INFO] Użyto lokalnego połączenia MySQL.");
    }
});

// 4. Kontrolery i autoryzacja
builder.Services.AddControllers();
builder.Services.AddAuthorization();

// --- 5. Budowanie aplikacji ---
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

app.Run();
