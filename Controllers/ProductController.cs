using ArWidgetApi.Data;
using ArWidgetApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Bazowa ścieżka: /api/product
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Konstruktor - wstrzykiwanie kontekstu bazy danych (Dependency Injection)
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint: GET /api/product/models
        // Ta metoda zastępuje ładowanie statycznego JSON z GitHub Pages
        [HttpGet("models")]
        public async Task<IActionResult> GetClientProducts()
        {
            // 1. Odbiór Tokenu Klienta z nagłówka (X-Client-Token)
            if (!Request.Headers.TryGetValue("X-Client-Token", out var clientTokenHeader))
            {
                // Zabezpieczenie: Jeśli brakuje tokenu, odmawiamy dostępu
                return Unauthorized(new { error = "Token klienta jest wymagany (X-Client-Token)." });
            }
            string clientToken = clientTokenHeader.ToString();

            // 2. Weryfikacja Tokenu i Statusu Subskrypcji
            // Wyszukujemy Klienta w naszej bazie
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                // Zabezpieczenie: Jeśli token nie istnieje lub subskrypcja wygasła
                return Unauthorized(new { error = "Nieprawidłowy token lub subskrypcja nieaktywna." });
            }

            // 3. Pobranie Modeli Powiązanych z Klientem
            var products = await _context.Products
                .Where(p => p.ClientId == client.Id)
                .Select(p => new // Mapujemy na Anonimowy Obiekt, aby dane były zgodne z oczekiwaniem Front-endu
                {
                    productId = p.ProductSku, // Ważne: Zgodne z kluczem w JS
                    name = p.Name,
                    description = p.Name, // Możesz dodać oddzielne pole 'description'
                    glb = p.ModelUrlGlb,
                    usdz = p.ModelUrlUsdz,
                    alt_text = p.Name
                })
                .ToListAsync();

            if (!products.Any())
            {
                return NotFound(new { message = "Brak skonfigurowanych produktów dla tego klienta." });
            }

            // Zwracamy listę produktów jako JSON do Front-endu
            return Ok(products);
        }
    }
}