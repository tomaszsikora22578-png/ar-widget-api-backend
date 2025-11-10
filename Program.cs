using ArWidgetApi.Models;
using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORS ---
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

// --- 2. Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 3. Baza danych MySQL (lokalnie i Cloud SQL) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// W Cloud Run ta zmienna jest ustawiana automatycznie
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

// Jeśli jesteśmy w Cloud Run – użyjemy Unix Socket
if (isCloudRun)
{
    // Budujemy connection string z Unix Socket
    var builderCloud = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
    {
        UnixSocket = $"/cloudsql/{cloudSqlInstance}"
    };
    connectionString = builderCloud.ConnectionString;
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

// --- 4. Kontrolery i autoryzacja ---
builder.Services.AddControllers();
builder.Services.AddAuthorization();

// --- 5. Build aplikacji ---
var app = builder.Build();

// --- 6. Middleware i swagger ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(ClientAppCORS);
app.UseAuthorization();

// Custom middleware do weryfikacji tokenów
app.UseMiddleware<ClientTokenMiddleware>();

app.MapControllers();
app.Run();
