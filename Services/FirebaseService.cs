using FirebaseAdmin.Auth;

namespace ArWidgetApi.Services
{
    public interface IFirebaseService
    {
        Task<string?> VerifyTokenAsync(string token);
    }

    public class FirebaseService : IFirebaseService
    {
        public async Task<string?> VerifyTokenAsync(string token)
        {
            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return decoded.Uid;
            }
            catch
            {
                return null;
            }
        }
    }
}
