using ArWidgetApi.Data;
using ArWidgetApi.Services; // Dodaj using dla GcsService
using ArWidgetApi.Models; // Dodaj using dla ModelDataDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Potrzebne do Where, Select, Any

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/product")] // Zmień na /api/product, jeśli ma być /api/product/models
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly GcsService _gcsService; // Wstrzykujemy nasz nowy serwis GCS

        // Konstruktor z wstrzykiwaniem dwóch serwisów
        public ProductController(ApplicationDbContext context, GcsService gcsService)
        {
            _context = context;
            _gcsService = gcsService;
        }

        // Endpoint: GET /api/product/models
        [HttpGet("models")]
        public async Task<IActionResult> GetClientProducts()
        {
            // 1. OdbiĂłr Tokenu Klienta z nagĹ‚Ăłwka
            if (!Request.Headers.TryGetValue("X-Client-Token", out var clientTokenHeader))
            {
                return Unauthorized(new { error = "Token klienta jest wymagany (X-Client-Token)." });
            }
            string clientToken = clientTokenHeader.ToString();

            // 2. Weryfikacja Tokenu i POBRANIE AUTORYZOWANYCH MODELI (JOIN EF CORE)
            // Używamy JOIN przez tabelę Client_Product_Access
            var productsQuery = _context.Clients
                .Where(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active")
                .SelectMany(c => c.ClientProductAccess) // Zakładamy relację w DbContext
                .Select(cpa => cpa.Product) // Pobieramy obiekty Product
                .Select(p => new ModelDataDto // Mapujemy bezpośrednio na DTO
                {
                    Name = p.Name,
                    ModelUrlGlb = p.ModelUrlGlb, // Ścieżki GCS z bazy
                    ModelUrlUsdz = p.ModelUrlUsdz,
                });
                
            var authorizedModels = await productsQuery.ToListAsync();

            if (!authorizedModels.Any())
            {
                // To obejmuje brak klienta/subskrypcji lub brak przypisanych modeli
                return NotFound(new { message = "Brak skonfigurowanych produktów dla tego klienta lub token jest nieprawidłowy." });
            }

            // 3. GENEROWANIE SIGNED URLS I CZYSZCZENIE ŚCIEŻEK
            foreach (var model in authorizedModels)
            {
                // GLB (Android)
                if (!string.IsNullOrEmpty(model.ModelUrlGlb))
                {
                    model.SignedUrlGlb = _gcsService.GenerateSignedUrl(model.ModelUrlGlb);
                    model.ModelUrlGlb = null; // Czyszczenie
                }

                // USDZ (iOS)
                if (!string.IsNullOrEmpty(model.ModelUrlUsdz))
                {
                    model.SignedUrlUsdz = _gcsService.GenerateSignedUrl(model.ModelUrlUsdz);
                    model.ModelUrlUsdz = null; // Czyszczenie
                }
            }

            // 4. Zwracamy listę produktów jako JSON (ModelDataDto)
            return Ok(authorizedModels);
        }
    }
}
