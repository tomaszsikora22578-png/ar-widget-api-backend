namespace ArWidgetApi.Services
{
    public interface IJwtsService
    {
        string CreateToken(string uid);
    }

    public class JwtsService : IJwtsService
    {
        public string CreateToken(string uid)
        {
            // Minimalny fake token – jeśli używasz Firebase ID Token,
            // to ten serwis może nawet nie być używany.
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(uid));
        }
    }
}
