using FirebaseAdmin.Auth;

namespace ArWidgetApi.Services
{
public interface IFirebaseAuthService
{
    Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
}

public class FirebaseAuthService : IFirebaseAuthService
{
    public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            return decodedToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Nieprawidłowy token: {ex.Message}");
            return null;
        }
    }
}
}
