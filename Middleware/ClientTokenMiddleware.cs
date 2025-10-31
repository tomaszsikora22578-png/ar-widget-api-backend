using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
namespace ArWidgetApi.Middleware // Ta przestrzeń nazw jest kluczowa!
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
            // 1. Sprawdzenie, czy żądanie ma nagłówek X-Client-Token
            if (!context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                // Jeśli nagłówka brakuje, zwracamy 401 Unauthorized
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
                // Jeśli token jest nieprawidłowy lub subskrypcja nieaktywna, zwracamy 401
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid client token or inactive subscription.");
                return;
            }

            // 3. Kontynuowanie żądania (przekazanie do kontrolera)
            await _next(context);
        }
    }
}