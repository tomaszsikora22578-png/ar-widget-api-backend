using ArWidgetApi.Data;
using ArWidgetApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Upewnij się, że to jest

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Bazowa sciezka /api/analytics
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsController> _logger; // Dodanie loggera

        public AnalyticsController(ApplicationDbContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Endpoint: POST /api/analytics/track
        [HttpPost("track")]
        public async Task<IActionResult> TrackEvent([FromBody] AnalyticsDto data)
        {
            // 1. POBRANIE CLIENT ID Z KONTEKSTU HTTP (ustawione przez ClientTokenMiddleware)
            if (!HttpContext.Items.TryGetValue("ClientId", out var clientIdObj) || !(clientIdObj is int clientId))
            {
                // To powinno być przechwycone przez Middleware, ale to jest zabezpieczenie.
                _logger.LogWarning("Błąd uwierzytelnienia. Brak ClientId w kontekście.");
                return Unauthorized(); 
            }

            // 2. Prosta walidacja danych
            if (data == null || data.ProductId <= 0)
            {
                _logger.LogWarning("Otrzymano nieprawidłowe dane analityczne. ProductId: {ProductId}", data?.ProductId);
                return BadRequest(new { error = "Wymagane ProductId." });
            }
            
            try
            {
                // 3. Zapis Zdarzenia do Bazy Danych
                var entry = new AnalyticsEntry
                {
                    // Używamy ClientId PRZETWORZONEGO przez Middleware
                    ClientId = clientId, 
                    ProductId = data.ProductId,
                    EventType = "AR_CLICK", 
                    Timestamp = DateTime.UtcNow 
                };

                _context.AnalyticsEntries.Add(entry);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Zdarzenie AR_CLICK zapisane: ClientId={ClientId}, ProductId={ProductId}", clientId, data.ProductId);

                // 4. Odpowiedź
                return NoContent(); // 204
            }
            catch (DbUpdateException ex)
            {
                // Błąd bazy danych (np. naruszenie NOT NULL, długość ciągu)
                _logger.LogError(ex, "🛑 BŁĄD ZAPISU DO DB (DbUpdateException): Nie udało się zapisać zdarzenia AR_CLICK.");
                return StatusCode(500, new { error = "Wewnętrzny błąd bazy danych podczas zapisu analityki." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🛑 NIEOCZEKIWANY BŁĄD serwera.");
                return StatusCode(500, new { error = "Wystąpił nieoczekiwany błąd serwera." });
            }
        }
    }
}
