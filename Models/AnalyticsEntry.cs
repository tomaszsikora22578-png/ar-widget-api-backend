namespace ArWidgetApi.Models
{
    public class AnalyticsEntry
    {
       public int Id { get; set; }
        public int ClientId { get; set; } // FK
        // ✅ POPRAWKA CS8618: Inicjalizacja domyślna dla relacji
        public Client Client { get; set; } = default!; 
        public int ProductId { get; set; } 
        public string EventType { get; set; } = "AR_CLICK";
        public DateTime Timestamp { get; set; }
    }
}
