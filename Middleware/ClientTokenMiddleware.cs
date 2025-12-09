using ArWidgetApi;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArWidgetApi.Middleware
{
    public class ClientTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private const string LogPath = "/api/Log/trackandserve";
        private const string AnalyticsPath = "/api/analytics/track";
        private const string ClientIdItemKey = "ClientId";

        public ClientTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            try
            {
                // 🔹 Przepuść preflight CORS (OPTIONS)
                if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                // 🔹 Pomiń LogController
                if (context.Request.Path.StartsWithSegments(LogPath, StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                // 🔹 Pomiń ContactController
                if (context.Request.Path.StartsWithSegments("/api/Contact/submit", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                string? clientToken = null;

                // 🔹 POST analityki
                if (context.Request.Path.StartsWithSegments(AnalyticsPath, StringComparison.OrdinalIgnoreCase) &&
                    context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    clientToken = context.Request.Query["token"].FirstOrDefault()?.Trim();
                }

                // 🔹 Authorization header
                if (string.IsNullOrEmpty(clientToken) &&
                    context.Request.Headers.TryGetValue("Authorization", out var authValues))
                {
                    var authHeader = authValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        clientToken = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }

                // 🔹 X-Client-Token header
                if (string.IsNullOrEmpty(clientToken) &&
                    context.Request.Headers.TryGetValue("X-Client-Token", out var tokenValues))
                {
                    clientToken = tokenValues.FirstOrDefault()?.Trim();
                }

                Console.WriteLine($"[ClientTokenMiddleware] Incoming: {context.Request.Method} {context.Request.Path}");
                Console.WriteLine($"[ClientTokenMiddleware] Token: {clientToken ?? "(brak)"}");

                if (string.IsNullOrEmpty(clientToken))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Client token is required.\"}");
                    return;
                }

                // 🔹 Weryfikacja tokena w bazie – bezpiecznie dla null
                var client = await dbContext.Clients
                    .Where(c => c.ClientToken != null &&
                                c.ClientToken.ToLower() == clientToken.ToLower() &&
                                c.SubscriptionStatus == "Active")
                    .FirstOrDefaultAsync();

                if (client == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Invalid client token or inactive subscription.\"}");
                    Console.WriteLine($"[ClientTokenMiddleware] ❌ Token invalid or subscription inactive: {clientToken}");
                    return;
                }

                // 🔹 Token OK – przekazanie ClientId
                context.Items[ClientIdItemKey] = client.Id;
                Console.WriteLine($"[ClientTokenMiddleware] ✅ Token accepted: {clientToken} (ClientId={client.Id})");

                await _next(context);
            }
            catch (Exception ex)
            {
                // 🔹 Pełny stack trace do logów
                Console.WriteLine($"[ClientTokenMiddleware] ⚠️ Exception: {ex}");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Server error during token validation.\"}");
            }
        }
    }
}
