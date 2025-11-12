using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi.Middleware
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
            // 🔹 Przepuść OPTIONS — potrzebne dla CORS preflight
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string? clientToken = null;

            // 🔹 1. Nagłówek X-Client-Token
            if (context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                clientToken = tokenValues.FirstOrDefault();
            }

            // 🔹 2. Lub Authorization: Bearer <token>
            else if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var authHeader = authValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    clientToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // 🔹 3. Brak tokena
            if (string.IsNullOrEmpty(clientToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Brak tokena klienta (X-Client-Token lub Authorization)." });
                return;
            }

            // 🔹 4. Weryfikacja w bazie
            var client = await dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Nieprawidłowy token lub subskrypcja nieaktywna." });
                return;
            }

            // 🔹 5. OK → przepuść dalej
            await _next(context);
        }
    }
}
