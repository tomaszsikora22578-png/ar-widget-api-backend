using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

        public AdminController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _adminUids = configuration.GetSection("AdminUsers:FirebaseUids").Get<List<string>>() ?? new List<string>();
        }

        // Logika autoryzacji: Sprawdzenie, czy zalogowany użytkownik jest na liście adminów
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
                // 🚨 Zmiana: Użycie ClientProductAccess zamiast Products
                var clients = await _db.Clients
                    .Include(c => c.ClientProductAccess)
                        .ThenInclude(cpa => cpa.Product) // Zakładamy, że ClientProductAccess ma referencję do Product
                    .Select(c => new ClientDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SubscriptionStatus = c.SubscriptionStatus,
                        ClientToken = c.ClientToken,
                        // Wybieramy SKU z modelu pośredniczącego
                        ProductSkus = c.ClientProductAccess
                                        .Select(cpa => cpa.Product.ProductSku) 
                                        .ToList() 
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
                string newToken = Guid.NewGuid().ToString("N");

                var newClient = new Client 
                {
                    Name = clientDto.Name ?? string.Empty,
                    SubscriptionStatus = clientDto.SubscriptionStatus ?? "Trial",
                    ClientToken = newToken,
                    ClientProductAccess = new List<ClientProductAccess>() // Inicjalizacja kolekcji
                };

                _db.Clients.Add(newClient);
                await _db.SaveChangesAsync();
                
                return CreatedAtAction(nameof(GetClients), new { id = newClient.Id }, new ClientDto 
                {
                    Id = newClient.Id,
                    Name = newClient.Name,
                    SubscriptionStatus = newClient.SubscriptionStatus,
                    ClientToken = newClient.ClientToken,
                    ProductSkus = new List<string>()
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
                return NoContent();
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
            
            // 🚨 Zmiana: Ładowanie ClientProductAccess i sprawdzenie
            var client = await _db.Clients
                                  .Include(c => c.ClientProductAccess) 
                                  .FirstOrDefaultAsync(c => c.Id == clientId);
            var product = await _db.Products.FindAsync(productId);

            if (client == null || product == null) 
            {
                return NotFound("Klient lub Produkt nie został znaleziony.");
            }

            // Sprawdzenie, czy relacja już istnieje
            if (client.ClientProductAccess.Any(cpa => cpa.ProductId == productId))
            {
                 return BadRequest("Produkt jest już przypisany do tego klienta.");
            }
            
            // Tworzenie i przypisanie nowego obiektu pośredniczącego
            client.ClientProductAccess.Add(new ClientProductAccess { ProductId = productId, ClientId = clientId });
            
            await _db.SaveChangesAsync();
            return Ok(new { Message = $"Produkt {productId} został przypisany do klienta {clientId}." });
        }
        
        // ================= PRODUCTS & ANALYTICS =================

        // GET /api/admin/products
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

        // GET /api/admin/analytics
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
    
    // Używane do tworzenia nowego klienta
    public class ClientCreateDto
    {
        public string Name { get; set; } = string.Empty; 
        public string SubscriptionStatus { get; set; } = "Trial";
    }
    
    // Zaktualizowane DTO dla widoku Klientów
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = string.Empty;
        public string ClientToken { get; set; } = string.Empty; 
        public List<string> ProductSkus { get; set; } = new List<string>(); // Wyświetlanie przypisanych produktów
    }

    // DTO dla produktów
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductSku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // DTO dla wpisów analitycznych
    public class AnalyticsDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int ProductId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
    
    // 🚨 Wymaga istnienia tej klasy (ClientProductAccess), jeśli nie była podana wcześniej
    public class ClientProductAccess
    {
        public int ClientId { get; set; }
        public Client Client { get; set; } = default!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;
    }
}
