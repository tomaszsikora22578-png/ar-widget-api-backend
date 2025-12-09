using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Services;


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
// 2) Config z Secret Manager / Env
// ========================
var firebaseKeyJson = builder.Configuration["firebase-admin-key"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ========================
// 3) Firebase Admin
// ========================
if (string.IsNullOrEmpty(firebaseKeyJson))
{
    Console.WriteLine("❌ Brak firebase-admin-key! Sprawdź Secret Manager w Cloud Run.");
}
else
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

// ========================
// 4) DATABASE
// ========================
builder.Services.AddDbContext<DataContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        Console.WriteLine("❌ Brak ConnectionString DefaultConnection");
});

// ========================
// 5) Services
// ========================
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<JwtsService>();
builder.Services.AddControllers();

// ========================
// 6) App
// ========================
var app = builder.Build();

// ❌ NIE WOLNO TEGO W CLOUD RUN
// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Endpoint zdrowia dla Cloud Run
app.MapGet("/", () => "API działa OK ✔️");

// ========================
// 7) Start
// ========================
Console.WriteLine($"🚀 API startuje na porcie {port}");
app.Run();
