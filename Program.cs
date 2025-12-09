using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Services;
using ArWidgetApi;

var builder = WebApplication.CreateBuilder(args);

// ========================
// 1) Wymuszony PORT Cloud Run
// ========================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// ========================
// 2) CORS dla frontendu demo
// ========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("https://intelicore.pl")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ========================
// 3) Config z Secret Manager / Env
// ========================
var firebaseKeyJson = builder.Configuration["firebase-admin-key"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ========================
// 4) Firebase Admin (opcjonalnie, jeśli jest klucz)
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
    Console.WriteLine("❌ Brak firebase-admin-key! Sprawdź Secret Manager w Cloud Run.");
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
// 6) Services
// ========================
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<JwtsService>();
builder.Services.AddControllers();

var app = builder.Build();

// ========================
// 7) Middleware i routing
// ========================

// 🔹 Routing musi być pierwszy
app.UseRouting();

// 🔹 Najpierw CORS, żeby OPTIONS działały
app.UseCors("AllowFrontend");

// 🔹 Potem Twój middleware walidacji tokena
app.UseMiddleware<ClientTokenMiddleware>();

// 🔹 Potem autoryzacja, jeśli używasz [Authorize]
app.UseAuthorization();

// 🔹 Mapowanie kontrolerów
app.MapControllers();

// Endpoint zdrowia dla Cloud Run
app.MapGet("/", () => "API działa OK ✔️");

// ========================
// 8) Start
// ========================
Console.WriteLine($"🚀 API startuje na porcie {port}");
app.Run();
