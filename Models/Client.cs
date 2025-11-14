namespace ArWidgetApi.Models
{
    public class Client
    {
        public int Id { get; set; } // PK
        public string ClientToken { get; set; } // KLUCZ - uzywany przez JS
        public string Name { get; set; }
        public string SubscriptionStatus { get; set; } = "Active"; // W przyszlosci: Active/Expired
        public ICollection<ClientProductAccess> ClientProductAccess { get; set; }
    }
}
