using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ArWidgetApi.Models;
using ArWidgetApi;
using System.Security.Claims;

namespace ArWidgetApi.Controllers
{
    // 🚨 KRUCJALNA ZMIANA: Wymuszenie weryfikacji tokena Firebase ID 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("/api/admin/clients")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly List<string> _adminUids;

        public AdminController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;

            // Pobranie UID adminów z konfiguracji (np. appsettings.json lub Secret Manager)
            _adminUids = configuration.GetSection("AdminUsers:FirebaseUids").Get<List<string>>() ?? new List<string>();
        }

        // ✅ Zaktualizowana logika: Sprawdzenie, czy zalogowany użytkownik jest na liście adminów
        // Dane pobierane są z CLAIMS (Payload tokena), a nie z HttpContext.Items
        private bool IsAdmin()
        {
            // Firebase ID Token zapisuje UID użytkownika w claimie "user_id" lub "sub"
            var firebaseUidClaim = User.FindFirst("user_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (firebaseUidClaim != null)
            {
                var uid = firebaseUidClaim.Value;
                return _adminUids.Contains(uid);
            }
            return false;
        }

        // ================= Endpoints =================

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            // 🚨 Autoryzacja: Token jest ważny, teraz sprawdzamy, czy użytkownik jest adminem
            if (!IsAdmin()) 
            {
                // Wracamy z błędem 403 Forbidden - użytkownik jest zalogowany (token ważny), ale nie ma uprawnień.
                return Forbid(); 
            }

            var clients = await _db.Clients
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SubscriptionStatus = c.SubscriptionStatus
                })
                .ToListAsync();

            return Ok(clients);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            if (!IsAdmin()) return Forbid();

            var products = await _db.Products
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProductSku = p.ProductSku,
                    AltText = p.AltText,
                    Description = p.Description
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            if (!IsAdmin()) return Forbid();

            var analytics = await _db.AnalyticsEntries
                .Select(a => new AnalyticsDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    ProductId = a.ProductId,
                    EventType = a.EventType,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            return Ok(analytics);
        }
    }

    // ================= DTO =================
    // (Klasy DTO są poza kontrolerem, ale dla kompletności pozostawione na końcu pliku)
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SubscriptionStatus { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductSku { get; set; }
        public string Name { get; set; }
        public string AltText { get; set; }
        public string Description { get; set; }
    }

    public class AnalyticsDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int ProductId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
