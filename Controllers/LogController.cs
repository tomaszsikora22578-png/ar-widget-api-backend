using ArWidgetApi.Data;
using ArWidgetApi.Models; 
using ArWidgetApi.Services; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class LogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        // Wstrzyknięcie GcsService
        private readonly GcsService _gcsService; 
        private readonly ILogger<LogController> _logger;

        public LogController(ApplicationDbContext context, GcsService gcsService, ILogger<LogController> logger)
        {
            _context = context;
            _gcsService = gcsService;
            _logger = logger;
        }

        // Endpoint: GET /api/log/trackandserve?token=...&productId=...&fileType=...
        [HttpGet("trackandserve")]
        public async Task<IActionResult> TrackAndServe(
            [FromQuery] string token, 
            [FromQuery] int productId, 
            [FromQuery] string fileType)
        {
            // --- 1. Uwierzytelnienie Klienta ---
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientToken == token && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                _logger.LogWarning("Odrzucono dostęp do pliku. Nieprawidłowy/nieaktywny token: {Token}", token);
                return Unauthorized("Nieprawidłowy token klienta.");
            }
            
            // --- 2. Logowanie Zdarzenia (AR_LOAD) ---
            try
            {
                var entry = new AnalyticsEntry
                {
                    ClientId = client.Id, 
                    ProductId = productId,
                    EventType = $"AR_LOAD_{fileType.ToUpper()}",
                    Timestamp = DateTime.UtcNow 
                };

                _context.AnalyticsEntries.Add(entry);
                // Użycie SaveChangesAsync gwarantuje, że zdarzenie jest logowane szybko
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ AR_LOAD zapisany: ClientId={ClientId}, ProductId={ProductId}, Type={FileType}", 
                    client.Id, productId, fileType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🛑 BŁĄD ZAPISU ANALEITYKI. Kontynuowanie serwowania pliku.");
            }

            // --- 3. Pobranie Ścieżki Modelu z Bazy ---
            var product = await _context.Products.FindAsync(productId);
            
            if (product == null)
            {
                 _logger.LogWarning("Produkt o Id={ProductId} nie istnieje. Nie można wygenerować linku.", productId);
                 return NotFound("Nie znaleziono produktu.");
            }
            
            string filePathInStorage = fileType.ToLower() == "glb" ? product.ModelUrlGlb : product.ModelUrlUsdz;

            if (string.IsNullOrEmpty(filePathInStorage))
            {
                 _logger.LogWarning("Brak ścieżki {FileType} dla produktu {ProductId}.", fileType, productId);
                 return NotFound("Brak pliku modelu.");
            }

            // --- 4. Generowanie Podpisanego URL i Przekierowanie ---
            // Używamy GcsService do wygenerowania podpisanego linku
            string actualSignedUrl = _gcsService.GenerateSignedUrl(
                filePathInStorage, 
                300 // 5 minut ważności
            );

            // Przekierowanie HTTP 302 Found
            return Redirect(actualSignedUrl); 
        }
        [HttpGet("trackArView")]
public async Task<IActionResult> TrackArView(string token, int productId)
{
    // 1. Walidacja tokenu i pobranie ClientId
    var client = await _context.Clients
        .SingleOrDefaultAsync(c => c.ClientToken == token && c.SubscriptionStatus == "Active");

    if (client == null)
    {
        return Unauthorized();
    }
    
    // 2. Utworzenie wpisu analitycznego z typem AR_VIEW
    var entry = new AnalyticsEntry
    {
        ClientId = client.Id,
        ProductId = productId,
        EventType = "AR_VIEW", // <-- NOWY TYP ZDARZENIA
        Timestamp = DateTime.UtcNow
    };

    // 3. Zapis do bazy
    await _context.AnalyticsEntries.AddAsync(entry);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation($"✅ AR_VIEW zapisany: ClientId={client.Id}, ProductId={productId}");

    // 4. Zwrócenie sukcesu bez treści (204 No Content)
    return NoContent();
}
    }
}
