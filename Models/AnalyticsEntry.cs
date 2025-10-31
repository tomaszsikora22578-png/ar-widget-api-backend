namespace ArWidgetApi.Models
{
    public class AnalyticsEntry
    {
        public int Id { get; set; }
        public int ClientId { get; set; } // FK
        public Client Client { get; set; }
        public string ProductId { get; set; } // Ktory produkt zostal klikniety
        public string EventType { get; set; } = "AR_CLICK";
        public DateTime Timestamp { get; set; }
    }
}
