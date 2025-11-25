namespace ArWidgetApi
{// Klasa do transferu danych z Front-endu do Back-endu
    public class AnalyticsDto
    {
        public string ClientToken { get; set; } // Z tokenu wiemy, ktĂłry klient
        public int ProductId { get; set; }   // SKU klikniÄ™tego produktu
        // W przyszĹ‚oĹ›ci mozesz dodac EventType, np. "3D_View"
    }
}
