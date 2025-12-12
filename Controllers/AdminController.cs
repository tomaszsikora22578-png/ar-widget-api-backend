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
                    .Include(c => c.ClientProductAccess)
                        .ThenInclude(cpa => cpa.Product)
                    .Select(c => new ClientDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SubscriptionStatus = c.SubscriptionStatus,
                        ClientToken = c.ClientToken,
                        // 🚨 POPRAWKA: Używamy product_id w LINQ do dostępu do SKU
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
                    ClientProductAccess = new List<ClientProductAccess>() 
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
            
            var client = await _db.Clients
                                  .Include(c => c.ClientProductAccess) 
                                  .FirstOrDefaultAsync(c => c.Id == clientId);
            var product = await _db.Products.FindAsync(productId);

            if (client == null || product == null) 
            {
                return NotFound("Klient lub Produkt nie został znaleziony.");
            }

            // Sprawdzenie, czy relacja już istnieje
            // 🚨 POPRAWKA: Używamy product_id zamiast ProductId
            if (client.ClientProductAccess.Any(cpa => cpa.product_id == productId))
            {
                 return BadRequest("Produkt jest już przypisany do tego klienta.");
            }
            
            // Tworzenie i przypisanie nowego obiektu pośredniczącego
            // 🚨 POPRAWKA: Używamy product_id i client_id
            client.ClientProductAccess.Add(new ClientProductAccess 
            { 
                product_id = productId, 
                client_id = clientId 
            });
            
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
