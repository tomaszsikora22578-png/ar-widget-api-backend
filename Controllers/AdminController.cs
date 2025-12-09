using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArWidgetApi.Models;
using ArWidgetApi;

namespace ArWidgetApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly List<string> _adminUids;

        public AdminController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;

            // Pobranie UID adminów z konfiguracji (appsettings.json lub Secret Manager)
            _adminUids = configuration.GetSection("AdminUsers:FirebaseUids").Get<List<string>>() ?? new List<string>();
        }

        // ✅ Sprawdzenie czy zalogowany użytkownik jest adminem
        private bool IsAdmin()
        {
            if (HttpContext.Items.TryGetValue("FirebaseUid", out var uid))
            {
                return _adminUids.Contains(uid?.ToString());
            }
            return false;
        }

        // ================= Endpoints =================

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            if (!IsAdmin()) return Unauthorized();

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
            if (!IsAdmin()) return Unauthorized();

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
            if (!IsAdmin()) return Unauthorized();

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
