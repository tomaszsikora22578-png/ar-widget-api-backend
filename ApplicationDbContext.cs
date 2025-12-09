using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Services;
using ArWidgetApi.Middleware;
using ArWidgetApi;

var builder = WebApplication.CreateBuilder(args);

// ========================
// 1) PORT Cloud Run
// ========================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// ========================
// 2) CORS
// ========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend3D", policy =>
    {
        policy.WithOrigins("https://intelicore.pl")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // jeśli token w header
    });

    options.AddPolicy("AdminPanel", policy =>
    {
        policy.WithOrigins(
            "http://127.0.0.1:5500",
            "https://tomaszsikora22578-png.github.io",
            "https://ar-widget-project.firebaseapp.com",
            "https://ar-widget-project.web.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ========================
// 3) Firebase (opcjonalnie)
// ========================
var firebaseKeyJson = builder.Configuration["firebase-admin-key"];
if (!string.IsNullOrEmpty(firebaseKeyJson))
{
    try
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(firebaseKeyJson)
        });
        Console.WriteLine("🔥 Firebase Admin – OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Firebase init error: " + ex.Message);
    }
}
else
{
    Console.WriteLine("⚠️ Brak firebase-admin-key, panel admina może działać bez loginu Firebase");
}

// ========================
// 4) DATABASE
// ========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];

if (!string.IsNullOrEmpty(cloudSqlInstance))
{
    // Cloud SQL via UNIX socket
    connectionString = $"Server=/cloudsql/{cloudSqlInstance};Database=ArWidgetDb;Uid=ar-widget-mysql;Pwd=YOUR_PASSWORD;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        Console.WriteLine("❌ Brak DefaultConnection");
});

// ========================
// 5) Services
// ========================
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<JwtsService>();
builder.Services.AddScoped<GcsService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================
// 6) Build app
// ========================
var app = builder.Build();

// ========================
// 7) Middleware
// ========================
app.UseRouting();

// CORS PRZED ClientTokenMiddleware
app.UseCors(policyBuilder =>
{
    var origin = app.Configuration["Origin"] ?? "";
    if (origin == "https://intelicore.pl")
        policyBuilder.WithOrigins("https://intelicore.pl").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    else
        policyBuilder.WithOrigins(
            "http://127.0.0.1:5500",
            "https://tomaszsikora22578-png.github.io",
            "https://ar-widget-project.firebaseapp.com",
            "https://ar-widget-project.web.app"
        ).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
});

// Middleware walidacji tokena
app.UseMiddleware<ClientTokenMiddleware>();

// Autoryzacja / kontrolery
app.UseAuthorization();
app.MapControllers();

// Swagger tylko w development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint zdrowia
app.MapGet("/", () => "API działa OK ✔️");

// ========================
// 8) Logowanie endpointów (debug)
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
Console.WriteLine("=== Koniec listy endpointów ===");

// ========================
// 9) Start
Console.WriteLine($"🚀 API startuje na porcie {port}");
app.Run();
