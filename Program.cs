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
// 2) CORS dla frontendu demo i panelu admina
// ========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://127.0.0.1:5500",
            "https://tomaszsikora22578-png.github.io",
            "https://ar-widget-project.firebaseapp.com",
            "https://ar-widget-project.web.app",
            "https://intelicore.pl"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
        // .AllowCredentials() - niepotrzebne do fetch / tokenów w nagłówkach
    });
});

// ========================
// 3) Config z Secret Manager / Env
// ========================
var firebaseKeyJson = builder.Configuration["firebase-admin-key"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cloudSqlInstance = builder.Configuration["CLOUD_SQL_CONNECTION_NAME"];

// Jeśli Cloud Run i Cloud SQL, użyj socketu
if (!string.IsNullOrEmpty(cloudSqlInstance))
{
    connectionString = $"Server=/cloudsql/{cloudSqlInstance};Database=ArWidgetDb;Uid=ar-widget-mysql;Pwd=0S3I5ggLGtP71c]V;";
    Console.WriteLine($"[INFO] Cloud SQL via UNIX socket: {cloudSqlInstance}");
}
else
{
    Console.WriteLine("[INFO] Użyto lokalnego połączenia MySQL.");
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
        Console.WriteLine("🔥 Firebase Admin – załadowany OK!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Błąd inicjalizacji Firebase: " + ex.Message);
    }
}
else
{
    Console.WriteLine("⚠️ Brak firebase-admin-key, panel admina będzie niedostępny.");
}

// ========================
// 5) DbContext
// ========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure();
        });
    else
        Console.WriteLine("❌ Brak ConnectionString DefaultConnection");
});

// ========================
// 6) Serwisy
// ========================
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<JwtsService>();
builder.Services.AddSingleton<GcsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================
// 7) Build aplikacji
// ========================
var app = builder.Build();

// Swagger tylko w development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ========================
// 8) Middleware kolejność
// ========================
app.UseRouting();

// CORS musi być PRZED middleware tokenowym
app.UseCors("AllowFrontend");

// Middleware autoryzacji tokenem klienta
app.UseMiddleware<ClientTokenMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Endpoint zdrowia
app.MapGet("/", () => "API działa OK ✔️");

// ========================
// 9) Start
// ========================
Console.WriteLine($"🚀 API startuje na porcie {port}");
app.Run();
