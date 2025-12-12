using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ArWidgetApi.Models;
using ArWidgetApi;
using System.Security.Claims;

namespace ArWidgetApi.Controllers
{
    // 🚨 KOREKTA ROUTINGU: Zmieniono na "api/admin", aby uniknąć podwójnego "clientsclients"
    [Route("api/admin")] 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly List<string> _adminUids;

        public AdminController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;

            // Pobranie UID adminów z konfiguracji (np. appsettings.json lub Secret Manager)
            //_adminUids = configuration.GetSection("AdminUsers:FirebaseUids").Get<List<string>>() ?? new List<string>();
       _adminUids = new List<string> { "mrlxV5NHoRMtRIj92qGfpLzLlpJ3" };
        }

        // ✅ Logika autoryzacji: Sprawdzenie, czy zalogowany użytkownik jest na liście adminów
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

        // Pełna ścieżka: GET /api/admin/clients
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            // 🚨 Autoryzacja: Sprawdzanie uprawnień admina
            if (!IsAdmin()) 
            {
                // Zwraca 403 Forbidden
                return Forbid(); 
            }

            try
            {
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
            catch (Exception ex)
            {
                // Logowanie błędu bazy danych
                Console.WriteLine($"[ERROR] Database query failed (GetClients): {ex.Message}");
                // Zwraca 500 Internal Server Error
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych klientów z bazy.");
            }
        }

        // Pełna ścieżka: GET /api/admin/products
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database query failed (GetProducts): {ex.Message}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych produktów z bazy.");
            }
        }

        // Pełna ścieżka: GET /api/admin/analytics
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database query failed (GetAnalytics): {ex.Message}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych analitycznych z bazy.");
            }
        }
    }

    // ================= DTO =================
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
