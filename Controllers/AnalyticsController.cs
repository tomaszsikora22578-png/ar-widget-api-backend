using ArWidgetApi.Data;
using ArWidgetApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Bazowa sciezka /api/analytics
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint: POST /api/analytics/track
        // Rejestruje klikniecie przycisku AR
        [HttpPost("track")]
        public async Task<IActionResult> TrackEvent([FromBody] AnalyticsDto data)
        {
            // 1. Walidacja Tokenu (dla bezpieczeństwa i przypisania rekordu)
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientToken == data.ClientToken);

            if (client == null)
            {
                // Zabezpieczenie: Odrzucenie zapytania z nieznanego lub pustego tokenu
                return BadRequest(new { error = "Nieprawidłowy token klienta." });
            }

            // 2. Zapis Zdarzenia do Bazy Danych
            var entry = new AnalyticsEntry
            {
                ClientId = client.Id, // Zapisujemy bezpieczne ID z bazy
                ProductId = data.ProductId,
                EventType = "AR_CLICK", // Definicja typu zdarzenia
                Timestamp = DateTime.UtcNow // Kiedy zdarzenie nastąpiło
            };

            _context.AnalyticsEntries.Add(entry);
            await _context.SaveChangesAsync();

            // 3. Odpowiedź
            // Zwracamy status 204 (NoContent) - operacja przebiegła pomyślnie, ale nic nie zwracamy
            // To jest optymalne dla Front-endu - nie blokuje przeglądarki klienta
            return NoContent();
        }
    }
}