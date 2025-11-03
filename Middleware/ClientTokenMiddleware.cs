using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
namespace ArWidgetApi.Middleware // Ta przestrzeĹ„ nazw jest kluczowa!
{
    public class ClientTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public ClientTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // 1. Sprawdzenie, czy ĹĽÄ…danie ma nagĹ‚Ăłwek X-Client-Token
            if (!context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                // JeĹ›li nagĹ‚Ăłwka brakuje, zwracamy 401 Unauthorized
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Client token is required.");
                return;
            }

            var clientToken = tokenValues.FirstOrDefault();

            // 2. Wyszukanie klienta w bazie danych
            var client = await dbContext.Clients
                .FirstOrDefaultAsync(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                // JeĹ›li token jest nieprawidĹ‚owy lub subskrypcja nieaktywna, zwracamy 401
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid client token or inactive subscription.");
                return;
            }

            // 3. Kontynuowanie ĹĽÄ…dania (przekazanie do kontrolera)
            await _next(context);
        }
    }
}