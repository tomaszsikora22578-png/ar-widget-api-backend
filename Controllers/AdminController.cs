using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ✅ Pomocnicza metoda do sprawdzenia, czy użytkownik jest adminem
    private bool IsAdmin()
    {
        if (HttpContext.Items.TryGetValue("FirebaseUid", out var uid))
        {
            // Sprawdzenie w bazie lub lista UID w pamięci
            var admins = new List<string> { "firebase-uid-admin1", "firebase-uid-admin2" };
            return admins.Contains(uid.ToString());
        }
        return false;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        if (!IsAdmin()) return Unauthorized();
        var clients = await _db.Clients.ToListAsync();
        return Ok(clients);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        if (!IsAdmin()) return Unauthorized();
        var products = await _db.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        if (!IsAdmin()) return Unauthorized();
        var analytics = await _db.AnalyticsEntries.ToListAsync();
        return Ok(analytics);
    }
}
