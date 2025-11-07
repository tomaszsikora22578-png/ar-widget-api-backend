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
            // KLUCZOWA POPRAWKA DLA CORS
            // Żądania OPTIONS (Preflight) są wysyłane przez przeglądarkę BEZ tokena.
            // Musimy je przepuścić, aby middleware CORS mogło poprawnie zwrócić nagłówki.
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            
            string? clientToken = null;

            // 1. Spróbuj odczytać niestandardowy nagłówek X-Client-Token
            if (context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                clientToken = tokenValues.FirstOrDefault();
            }
            // 2. Jeśli brak, spróbuj Authorization: Bearer <token>
            else if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var authHeader = authValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    clientToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // 3. Jeśli nadal brak tokena -> 401 Unauthorized
            if (string.IsNullOrEmpty(clientToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Client token is required.");
                return;
            }

            // 4. Sprawdź token w bazie
            var client = await dbContext.Clients
                .FirstOrDefaultAsync(c => c.ClientToken == clientToken && c.SubscriptionStatus == "Active");

            if (client == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid client token or inactive subscription.");
                return;
            }

            // 5. Token OK -> przejdź dalej
            await _next(context);
        }
    }
}
