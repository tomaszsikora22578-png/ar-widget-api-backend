using Microsoft.AspNetCore.Http;
using ArWidgetApi.Services;
using System.Threading.Tasks;

namespace ArWidgetApi.Middleware
{
public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, FirebaseAuthService firebaseAuth)
    {
        // 🔥 1. Publiczne endpointy BEZ autoryzacji
        var path = context.Request.Path.Value?.ToLower();

        if (path == "/" ||
            path.Contains("/swagger") ||
            path.Contains("/status") ||
            path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // 🔥 2. Sprawdź nagłówek Authorization
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing Authorization header");
            return;
        }

        var token = authHeader.ToString().Replace("Bearer ", "").Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid Authorization header");
            return;
        }

        // 🔥 3. Weryfikacja tokenu Firebase
        var decodedToken = await firebaseAuth.VerifyIdTokenAsync(token);

        if (decodedToken == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or expired Firebase token");
            return;
        }

        // 🔥 4. Przekazujemy UID dalej
        context.Items["FirebaseUid"] = decodedToken.Uid;

        // 🔥 5. Kontynuujemy request
        await _next(context);
    }
}
}
