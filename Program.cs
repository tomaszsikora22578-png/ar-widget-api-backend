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
// 2) CORS – dwie polityki
// ========================
builder.Services.AddCors(options =>
{
    // Frontend demo 3D
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://intelicore.pl")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Panel admina
    options.AddPolicy("AllowAdmin", policy =>
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
// 3) Config z Secret Manager / Env
// ========================
var firebaseKeyJson = builder.Configuration["firebase-admin-key"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];
var isCloudRun = !string.IsNullOrEmpty(cloudSqlInstance);

if (isCloudRun)
{
    connectionString = $"Server=/cloudsql/{cloudSqlInstance};Database=ArWidgetDb;Uid=ar-widget-mysql;Pwd=0S3I5ggLGtP71c]V;";
}

// ========================
// 4) Firebase Admin (opcjonalnie)
// ========================
if (!string.IsNullOrEmpty(firebaseKeyJson))
{
    try
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(firebaseKeyJson)
        });
        Console.WriteLine("🔥 Firebase Admin – OK!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Błąd inicjalizacji Firebase: " + ex.Message);
    }
}
else
{
    Console.WriteLine("⚠️ Brak firebase-admin-key – panel admina będzie działał bez logowania Firebase");
}

// ========================
// 5) DATABASE
// ========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        Console.WriteLine("❌ Brak ConnectionString DefaultConnection");
});

// ========================
// 6) Serwisy
// ========================
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<JwtsService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================
// 7) Build app
// ========================
var app = builder.Build();

// ========================
// 8) Middleware
// ========================
app.UseRouting();

// 🔹 CORS musi być przed ClientTokenMiddleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Wybieramy politykę CORS w zależności od ścieżki
    if (path != null && path.StartsWith("/api/product")) // endpointy modeli 3D
        context.Response.Headers.Add("Access-Control-Allow-Origin", "https://intelicore.pl");
    else
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*"); // admin i inne

    await next();
});

// Lub prostsza metoda: możesz też użyć dedykowanych endpointów z UseCors("AllowFrontend")/UseCors("AllowAdmin") w mapowaniu

// Middleware autoryzacji tokena
app.UseMiddleware<ClientTokenMiddleware>();

// Autoryzacja
app.UseAuthorization();

// Swagger tylko w dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Mapowanie kontrolerów
app.MapControllers();

// Endpoint zdrowia
app.MapGet("/", () => "API działa OK ✔️");

// Debug endpointów
var dataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("=== Lista dostępnych endpointów ===");
foreach (var endpoint in dataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
Console.WriteLine("=== Koniec listy endpointów ===");

// ========================
// 9) Start
// ========================
Console.WriteLine($"🚀 API startuje na porcie {port}");
app.Run();
