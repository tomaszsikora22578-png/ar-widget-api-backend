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
            // 🔹 Przepuść preflight CORS (OPTIONS)
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string? clientToken = null;

            // 🔹 1. Spróbuj Authorization: Bearer <token>
            if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var authHeader = authValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    clientToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // 🔹 2. Jeśli brak, sprawdź X-Client-Token
            if (string.IsNullOrEmpty(clientToken) && context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
            {
                clientToken = tokenValues.FirstOrDefault()?.Trim();
            }

            // 🔹 Logowanie diagnostyczne
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine($"[ClientTokenMiddleware] Incoming request: {context.Request.Method} {context.Request.Path}");
            Console.WriteLine($"[ClientTokenMiddleware] Received Token: {clientToken ?? "(brak)"}");
            Console.WriteLine("-------------------------------------------------------");

            // 🔹 3. Brak tokena → 401
            if (string.IsNullOrEmpty(clientToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Client token is required.\"}");
                return;
            }

            try
            {
                // 🔹 4. Weryfikacja tokena w bazie (bez rozróżniania wielkości liter)
                var client = await dbContext.Clients
                    .FirstOrDefaultAsync(c =>
                        c.ClientToken.ToLower() == clientToken.ToLower() &&
                        c.SubscriptionStatus == "Active");

                if (client == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Invalid client token or inactive subscription.\"}");
                    Console.WriteLine($"[ClientTokenMiddleware] ❌ Token nieprawidłowy lub subskrypcja nieaktywna: {clientToken}");
                    return;
                }

                // 🔹 5. Token OK
                Console.WriteLine($"[ClientTokenMiddleware] ✅ Token zaakceptowany: {clientToken} (ClientId={client.Id})");

                await _next(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientTokenMiddleware] ⚠️ Błąd walidacji tokena: {ex.Message}");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Server error during token validation.\"}");
            }
        }
    }
}
