namespace ArWidgetApi
{// Klasa do transferu danych z Front-endu do Back-endu
    public class AnalyticsDto
    {
        public string ClientToken { get; set; } // Z tokenu wiemy, który klient
        public string ProductId { get; set; }   // SKU klikniętego produktu
        // W przyszłości mozesz dodac EventType, np. "3D_View"
    }
}
