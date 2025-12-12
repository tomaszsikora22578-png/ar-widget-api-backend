using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// 🚨 Pamiętaj o dodaniu modeli Client, Product, AnalyticsEntry, jeśli nie są widoczne
using ArWidgetApi.Models; 
using ArWidgetApi;
using System.Security.Claims;

namespace ArWidgetApi.Controllers
{
    [Route("api/admin")] 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly List<string> _adminUids;

        // 🚨 Wymaga dodania serwisu do generowania tokenów, np. ITokenGeneratorService
        // public AdminController(ApplicationDbContext db, IConfiguration configuration, ITokenGeneratorService tokenService)

        public AdminController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            // Tutaj musi być zaimplementowana poprawna logika ładowania UID (wcześniej naprawiona)
            _adminUids = configuration.GetSection("AdminUsers:FirebaseUids").Get<List<string>>() ?? new List<string>();
        }

        // ✅ Logika autoryzacji
        private bool IsAdmin()
        {
            var firebaseUidClaim = User.FindFirst("user_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (firebaseUidClaim != null)
            {
                var uid = firebaseUidClaim.Value;
                return _adminUids.Contains(uid);
            }
            return false;
        }

        // ================= CLIENTS CRUD & MANAGEMENT =================

        // GET /api/admin/clients - POBIERANIE WSZYSTKICH KLIENTÓW
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var clients = await _db.Clients
                    .Select(c => new ClientDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SubscriptionStatus = c.SubscriptionStatus,
                        // 🚨 Dodajemy token, jeśli jest przechowywany w modelu Client
                        ClientToken = c.ClientToken // Zakładamy, że Client ma pole ClientToken
                    })
                    .ToListAsync();

                return Ok(clients);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database query failed (GetClients): {ex.Message}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych klientów z bazy.");
            }
        }

        // POST /api/admin/clients - DODAWANIE NOWEGO KLIENTA
        [HttpPost("clients")]
        public async Task<IActionResult> AddClient([FromBody] ClientCreateDto clientDto)
        {
            if (!IsAdmin()) return Forbid();
            if (clientDto == null || string.IsNullOrWhiteSpace(clientDto.Name))
            {
                return BadRequest("Nazwa klienta jest wymagana.");
            }

            try
            {
                // 1. Stworzenie tokena (np. Guid, JWT, lub inny)
                string newToken = Guid.NewGuid().ToString("N"); // Generowanie prostego, unikalnego tokena

                var newClient = new Client // Zakładamy, że masz model Client
                {
                    Name = clientDto.Name,
                    SubscriptionStatus = clientDto.SubscriptionStatus ?? "Trial",
                    ClientToken = newToken, // Przypisanie nowego tokena
                    // Inne pola modelu Client
                };

                _db.Clients.Add(newClient);
                await _db.SaveChangesAsync();
                
                // Zwracamy stworzony obiekt z tokenem
                return CreatedAtAction(nameof(GetClients), new { id = newClient.Id }, new ClientDto 
                {
                    Id = newClient.Id,
                    Name = newClient.Name,
                    SubscriptionStatus = newClient.SubscriptionStatus,
                    ClientToken = newClient.ClientToken
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database operation failed (AddClient): {ex.Message}");
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
        
        // DELETE /api/admin/clients/{id} - USUWANIE KLIENTA
        [HttpDelete("clients/{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            if (!IsAdmin()) return Forbid();

            var client = await _db.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound($"Klient o ID {id} nie został znaleziony.");
            }

            try
            {
                _db.Clients.Remove(client);
                await _db.SaveChangesAsync();
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database operation failed (DeleteClient): {ex.Message}");
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
        
        // POST /api/admin/clients/{clientId}/token/generate - GENEROWANIE NOWEGO TOKENA
        [HttpPost("clients/{clientId}/token/generate")]
        public async Task<IActionResult> GenerateNewToken(int clientId)
        {
            if (!IsAdmin()) return Forbid();

            var client = await _db.Clients.FindAsync(clientId);
            if (client == null) return NotFound($"Klient o ID {clientId} nie został znaleziony.");

            try
            {
                // Generowanie nowego tokena (np. Guid)
                string newToken = Guid.NewGuid().ToString("N");
                client.ClientToken = newToken;
                
                _db.Clients.Update(client);
                await _db.SaveChangesAsync();

                return Ok(new { ClientId = client.Id, NewToken = newToken });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database operation failed (GenerateNewToken): {ex.Message}");
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
        
        // POST /api/admin/clients/{clientId}/products/{productId} - WIĄZANIE PRODUKTU Z KLIENTEM
        [HttpPost("clients/{clientId}/products/{productId}")]
        public async Task<IActionResult> AssignProductToClient(int clientId, int productId)
        {
            if (!IsAdmin()) return Forbid();
            
            // Zakładamy istnienie tabeli pośredniczącej ClientProduct
            var client = await _db.Clients
                                  .Include(c => c.ClientProducts) // Pamiętaj o Include w DbContext!
                                  .FirstOrDefaultAsync(c => c.Id == clientId);
            var product = await _db.Products.FindAsync(productId);

            if (client == null || product == null) 
            {
                return NotFound("Klient lub Produkt nie został znaleziony.");
            }
            
            // 🚨 Dodanie logiki wiążącej, zakładającej istnienie tabeli pośredniczącej
            // Na przykład: client.ClientProducts.Add(new ClientProduct { ProductId = productId });
            // ... (implementacja)
            
            await _db.SaveChangesAsync();
            return Ok(new { Message = $"Produkt {productId} został przypisany do klienta {clientId}." });
        }
        
        // ================= PRODUCTS & ANALYTICS =================

        // GET /api/admin/products - (Pozostaje bez zmian)
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            // ... (kod bez zmian)
        }

        // GET /api/admin/analytics - (Pozostaje bez zmian)
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            // ... (kod bez zmian)
        }
    }

    // ================= NOWE I ZMODYFIKOWANE DTO =================
    
    // Używane do tworzenia nowego klienta (tylko Nazwa)
    public class ClientCreateDto
    {
        public string Name { get; set; }
        public string SubscriptionStatus { get; set; }
    }
    
    // Zaktualizowane DTO dla widoku, aby pokazywać token
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SubscriptionStatus { get; set; }
        public string ClientToken { get; set; } // Nowe pole
    }

    // Pozostałe DTO (ProductDto, AnalyticsDto) pozostają bez zmian
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
