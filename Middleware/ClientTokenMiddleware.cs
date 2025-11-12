using ArWidgetApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

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
            // 1️⃣ Przepuść preflight (OPTIONS)
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return; // Nie sprawdzamy tokena dla preflight
            }

            string? clientToken = null;

            // 2️⃣ Spróbuj odczytać nagłówek X-Client-Token
            if (context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                clientToken = tokenValues.FirstOrDefault();
            }
            // 3️⃣ Jeśli brak, spróbuj Authorization: Bearer <token>
            else if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var authHeader = authValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    clientToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // 4️⃣ Brak tokena -> 401 Unauthorized
            if (string.IsNullOrEmpty(clientToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Client token is required.");
                return;
            }

            // 5️⃣ Sprawdź token w bazie
            var client = await dbContext.Clients
                .FirstOrDefaultAsync(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid client token or inactive subscription.");
                return;
            }

            // 6️⃣ Token OK -> przejdź dalej
            await _next(context);
        }
    }
}
