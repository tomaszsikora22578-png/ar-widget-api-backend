using Microsoft.AspNetCore.Http;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IFirebaseAuthService authService)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "");
            var decodedToken = await authService.VerifyIdTokenAsync(token);

            if (decodedToken != null)
            {
                // Przekazanie UID do dalszych endpointów
                context.Items["FirebaseUid"] = decodedToken.Uid;
            }
        }

        await _next(context);
    }
}
